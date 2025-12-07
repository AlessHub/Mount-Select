using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.IoC;
using System.Reflection;
using System.Diagnostics;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using MountSelect.Configuration;
using MountSelect.Services;
using MountSelect.Windows;
using System.Runtime.InteropServices;

namespace MountSelect;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;

    private const string CommandName = "/mountselect";
    private const string ConfigCommandName = "/mountconfig";
    private const string MountCommandName = "/qmount";
    private const string MultiMountCommandName = "/multimount";

    public PluginConfiguration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("MountSelect");
    private MainWindow MainWindow { get; init; }
    private ConfigWindow ConfigWindow { get; init; }

    private MountService MountService { get; init; }
    private MountActionService MountActionService { get; init; }


    private bool mountKeybindPressed = false;
    private bool multiMountKeybindPressed = false;
    

    private static class Native
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
    }

    public Plugin()
    {
        this.Configuration = PluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();

        var ownershipService = new MountOwnershipService(PluginLog);
        this.MountService = new MountService(DataManager, ClientState, PluginLog, ownershipService);
        this.MountActionService = new MountActionService(PluginLog, ChatGui, Condition, ClientState, MountService, CommandManager);

        this.MainWindow = new MainWindow(this, MountService, MountActionService);
        this.ConfigWindow = new ConfigWindow(this, MountService);

        this.WindowSystem.AddWindow(this.MainWindow);
        this.WindowSystem.AddWindow(this.ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open Mount Select window"
        });

        CommandManager.AddHandler(ConfigCommandName, new CommandInfo(this.OnConfigCommand)
        {
            HelpMessage = "Open Mount Select configuration"
        });

        CommandManager.AddHandler(MountCommandName, new CommandInfo(this.OnMountCommand)
        {
            HelpMessage = "Quick mount - summons your job's assigned mount (/qmount)"
        });

        CommandManager.AddHandler(MultiMountCommandName, new CommandInfo(this.OnMultiMountCommand)
        {
            HelpMessage = "Summon a multi-seat mount from your favorites (/multimount)"
        });

        PluginInterface.UiBuilder.Draw += this.DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += this.ToggleMainUI;
        PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigUI;
        Framework.Update += this.OnFrameworkUpdate;

        try
        {
            var amType = typeof(FFXIVClientStructs.FFXIV.Client.Game.ActionManager);
            var methodNames = amType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name).Distinct().OrderBy(n => n).ToArray();
            PluginLog.Info($"ActionManager methods ({methodNames.Length}): {string.Join(", ", methodNames)}");
        }
        catch (Exception ex)
        {
            PluginLog.Debug(ex, "Failed to reflect ActionManager methods");
        }

        PluginLog.Info("Mount Select plugin loaded successfully!");
    }

    public void Dispose()
    {
        this.WindowSystem.RemoveAllWindows();

        this.MainWindow?.Dispose();
        this.ConfigWindow?.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(ConfigCommandName);
        CommandManager.RemoveHandler(MountCommandName);
        CommandManager.RemoveHandler(MultiMountCommandName);
        

        PluginInterface.UiBuilder.Draw -= this.DrawUI;
        PluginInterface.UiBuilder.OpenMainUi -= this.ToggleMainUI;
        PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigUI;
        Framework.Update -= this.OnFrameworkUpdate;
    }

    private void OnCommand(string command, string args)
    {
        this.ToggleMainUI();
    }

    private void OnConfigCommand(string command, string args)
    {
        this.ToggleConfigUI();
    }

    private void OnMountCommand(string command, string args)
    {
        if (MountActionService.IsMounted())
        {
            MountActionService.Dismount();
            return;
        }
        
        MountActionService.SummonMountForCurrentJob(Configuration);
    }


    private void OnMultiMountCommand(string command, string args)
    {
        if (MountActionService.IsMounted())
        {
            MountActionService.Dismount();
            return;
        }
        
        if (Configuration.MultiSeatMounts == null || Configuration.MultiSeatMounts.Count == 0)
        {
            ChatGui.Print("No multi-seat mounts configured! Use /mountselect to add favorites.");
            return;
        }

        var random = new Random();
        var randomIndex = random.Next(Configuration.MultiSeatMounts.Count);
        var mountId = Configuration.MultiSeatMounts[randomIndex];
        MountActionService.SummonMount(mountId, Configuration);
    }

    

    private void DrawUI() => this.WindowSystem.Draw();

    public void ToggleMainUI() => this.MainWindow.Toggle();
    public void ToggleConfigUI() => this.ConfigWindow.Toggle();

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!ClientState.IsLoggedIn || Process.GetCurrentProcess().MainWindowHandle != Native.GetForegroundWindow())
        {
            return;
        }
        
        if (Configuration.EnableMountKeybind && Configuration.MountKeybindKey != 0)
        {
            bool keybindPressed = IsKeybindPressed(
                Configuration.MountKeybindKey,
                Configuration.MountKeybindCtrl,
                Configuration.MountKeybindAlt,
                Configuration.MountKeybindShift
            );

            if (keybindPressed && !mountKeybindPressed)
            {
                mountKeybindPressed = true;
                
                if (MountActionService.IsMounted())
                {
                    MountActionService.Dismount();
                }
                else
                {
                    MountActionService.SummonMountForCurrentJob(Configuration);
                }
            }
            else if (!keybindPressed)
            {
                mountKeybindPressed = false;
            }
        }

        if (Configuration.EnableMultiMountKeybind && Configuration.MultiMountKeybindKey != 0)
        {
            bool keybindPressed = IsKeybindPressed(
                Configuration.MultiMountKeybindKey,
                Configuration.MultiMountKeybindCtrl,
                Configuration.MultiMountKeybindAlt,
                Configuration.MultiMountKeybindShift
            );

            if (keybindPressed && !multiMountKeybindPressed)
            {
                multiMountKeybindPressed = true;
                OnMultiMountCommand("", "");
            }
            else if (!keybindPressed)
            {
                multiMountKeybindPressed = false;
            }
        }

        
    }

    private bool IsKeybindPressed(int key, bool requireCtrl, bool requireAlt, bool requireShift)
    {
        bool mainPressed;
        
        if (key == (int)VirtualKey.XBUTTON1 || key == (int)VirtualKey.XBUTTON2)
        {
            short asyncState = Native.GetAsyncKeyState(key);
            mainPressed = (asyncState & 0x8000) != 0;
            
            if (mainPressed)
            {
                PluginLog.Debug($"[Runtime Mouse Check] Key={key} ({(VirtualKey)key}), AsyncState=0x{asyncState:X4}, Pressed=TRUE");
            }
        }
        else
        {
            mainPressed = KeyState[(VirtualKey)key];
        }

        if (!mainPressed)
            return false;

        bool ctrlPressed = KeyState[VirtualKey.CONTROL];
        bool altPressed = KeyState[VirtualKey.MENU];
        bool shiftPressed = KeyState[VirtualKey.SHIFT];

        if (requireCtrl != ctrlPressed) return false;
        if (requireAlt != altPressed) return false;
        if (requireShift != shiftPressed) return false;

        return true;
    }
}