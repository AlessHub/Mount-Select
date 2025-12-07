using System;
using System.Runtime.InteropServices;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;

namespace MountSelect.Windows;

public partial class ConfigWindow
{
    private bool capturingMountKeybind = false;
    private bool capturingMultiMountKeybind = false;
    // arm time to allow side mouse buttons (XBUTTON1/2) after we click the "Set" button with LBUTTON
    // if we don't do this, the mouse button press will be captured immediately
    private DateTime? mouseCaptureReadyAt = null;

    private static class Native
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }

    private void DrawKeybindsSection()
    {
        ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.4f, 1.0f), "⌨ Keybinds");
        ImGui.TextWrapped("Assign hotkeys to quickly summon mounts without using commands.");
        ImGui.Spacing();

        var enableMountKeybind = configuration.EnableMountKeybind;
        if (ImGui.Checkbox("Enable Mount Keybind (/qmount)", ref enableMountKeybind))
        {
            configuration.EnableMountKeybind = enableMountKeybind;
            configuration.Save();
        }

        if (configuration.EnableMountKeybind)
        {
            ImGui.Indent();
            DrawKeybindSetter(
                "Mount Keybind",
                ref capturingMountKeybind,
                configuration.MountKeybindKey,
                configuration.MountKeybindCtrl,
                configuration.MountKeybindAlt,
                configuration.MountKeybindShift,
                (key, ctrl, alt, shift) =>
                {
                    if (configuration.EnableMultiMountKeybind &&
                        key == configuration.MultiMountKeybindKey &&
                        ctrl == configuration.MultiMountKeybindCtrl &&
                        alt == configuration.MultiMountKeybindAlt &&
                        shift == configuration.MultiMountKeybindShift)
                    {
                        Plugin.PluginLog.Warning("Cannot set mount keybind: same as multi-mount keybind");
                        Plugin.ChatGui.PrintError("This keybind is already used for Multi-Seat Mount!");
                        return;
                    }

                    configuration.MountKeybindKey = key;
                    configuration.MountKeybindCtrl = ctrl;
                    configuration.MountKeybindAlt = alt;
                    configuration.MountKeybindShift = shift;
                    configuration.Save();
                },
                () =>
                {
                    configuration.MountKeybindKey = 0;
                    configuration.MountKeybindCtrl = false;
                    configuration.MountKeybindAlt = false;
                    configuration.MountKeybindShift = false;
                    configuration.Save();
                }
            );
            ImGui.Unindent();
        }

        ImGui.Spacing();

        var enableMultiMountKeybind = configuration.EnableMultiMountKeybind;
        if (ImGui.Checkbox("Enable Multi-Seat Mount Keybind (/multimount)", ref enableMultiMountKeybind))
        {
            configuration.EnableMultiMountKeybind = enableMultiMountKeybind;
            configuration.Save();
        }

        if (configuration.EnableMultiMountKeybind)
        {
            ImGui.Indent();
            DrawKeybindSetter(
                "Multi-Mount Keybind",
                ref capturingMultiMountKeybind,
                configuration.MultiMountKeybindKey,
                configuration.MultiMountKeybindCtrl,
                configuration.MultiMountKeybindAlt,
                configuration.MultiMountKeybindShift,
                (key, ctrl, alt, shift) =>
                {
                    if (configuration.EnableMountKeybind &&
                        key == configuration.MountKeybindKey &&
                        ctrl == configuration.MountKeybindCtrl &&
                        alt == configuration.MountKeybindAlt &&
                        shift == configuration.MountKeybindShift)
                    {
                        Plugin.PluginLog.Warning("Cannot set multi-mount keybind: same as mount keybind");
                        Plugin.ChatGui.PrintError("This keybind is already used for Mount!");
                        return;
                    }

                    configuration.MultiMountKeybindKey = key;
                    configuration.MultiMountKeybindCtrl = ctrl;
                    configuration.MultiMountKeybindAlt = alt;
                    configuration.MultiMountKeybindShift = shift;
                    configuration.Save();
                },
                () =>
                {
                    configuration.MultiMountKeybindKey = 0;
                    configuration.MultiMountKeybindCtrl = false;
                    configuration.MultiMountKeybindAlt = false;
                    configuration.MultiMountKeybindShift = false;
                    configuration.Save();
                }
            );
            ImGui.Unindent();
        }
    }

    private void DrawKeybindSetter(
        string label,
        ref bool capturing,
        int currentKey,
        bool currentCtrl,
        bool currentAlt,
        bool currentShift,
        Action<int, bool, bool, bool> onKeybindSet,
        Action onClear)
    {
        string keybindText = FormatKeybind(currentKey, currentCtrl, currentAlt, currentShift);

        ImGui.Text($"{label}:");
        ImGui.SameLine();

        string buttonText;
        if (capturing)
        {
            if (mouseCaptureReadyAt.HasValue)
            {
                var remaining = mouseCaptureReadyAt.Value - DateTime.UtcNow;
                if (remaining.TotalMilliseconds > 0)
                {
                    buttonText = ">>> Wait... <<<";
                }
                else
                {
                    buttonText = ">>> Press any key <<<";
                }
            }
            else
            {
                buttonText = ">>> Press any key <<<";
            }
        }
        else
        {
            buttonText = keybindText;
        }

        ImGui.PushStyleColor(ImGuiCol.Button, capturing ? new Vector4(0.4f, 0.7f, 0.4f, 1.0f) : new Vector4(0.2f, 0.2f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, capturing ? new Vector4(0.5f, 0.8f, 0.5f, 1.0f) : new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, capturing ? new Vector4(0.3f, 0.6f, 0.3f, 1.0f) : new Vector4(0.15f, 0.15f, 0.15f, 1.0f));

        if (ImGui.Button($"{buttonText}###{label}_display", new Vector2(250, 0)))
        {
            capturing = !capturing;
            if (capturing)
            {
                mouseCaptureReadyAt = DateTime.UtcNow.AddMilliseconds(300);
                Plugin.PluginLog.Info($"🎯 Started capturing keybind for {label}");
            }
            else
            {
                Plugin.PluginLog.Info($"❌ Cancelled capturing keybind for {label}");
                mouseCaptureReadyAt = null;
            }
        }

        ImGui.PopStyleColor(3);

        if (!capturing && currentKey != 0)
        {
            ImGui.SameLine();
            if (ImGui.Button($"Clear###{label}_clear"))
            {
                onClear();
            }
        }

        if (capturing)
        {
            var keyState = Plugin.KeyState;

            if (keyState != null && keyState[VirtualKey.ESCAPE])
            {
                capturing = false;
                Plugin.PluginLog.Info("Keybind capture cancelled with ESC");
            }
            else if (keyState != null)
            {
                bool ctrl = keyState[VirtualKey.CONTROL];
                bool alt = keyState[VirtualKey.MENU];
                bool shift = keyState[VirtualKey.SHIFT];

                if (mouseCaptureReadyAt.HasValue && DateTime.UtcNow >= mouseCaptureReadyAt.Value)
                {
                    short xb1State = Native.GetAsyncKeyState((int)VirtualKey.XBUTTON1);
                    short xb2State = Native.GetAsyncKeyState((int)VirtualKey.XBUTTON2);

                    bool xb1 = (xb1State & 0x8000) != 0;
                    bool xb2 = (xb2State & 0x8000) != 0;

                    if (xb1 || xb2)
                    {
                        var mouseVk = xb1 ? VirtualKey.XBUTTON1 : VirtualKey.XBUTTON2;
                        int mouseCode = (int)mouseVk;
                        Plugin.PluginLog.Info($"✓ Captured side mouse button: {mouseVk}");
                        onKeybindSet(mouseCode, ctrl, alt, shift);
                        capturing = false;
                        mouseCaptureReadyAt = null;
                    }
                }

                VirtualKey[] keysToCheck = new[]
                {
                    VirtualKey.F1, VirtualKey.F2, VirtualKey.F3, VirtualKey.F4,
                    VirtualKey.F5, VirtualKey.F6, VirtualKey.F7, VirtualKey.F8,
                    VirtualKey.F9, VirtualKey.F10, VirtualKey.F11, VirtualKey.F12,
                    VirtualKey.KEY_0, VirtualKey.KEY_1, VirtualKey.KEY_2, VirtualKey.KEY_3,
                    VirtualKey.KEY_4, VirtualKey.KEY_5, VirtualKey.KEY_6, VirtualKey.KEY_7,
                    VirtualKey.KEY_8, VirtualKey.KEY_9,
                    VirtualKey.A, VirtualKey.B, VirtualKey.C, VirtualKey.D, VirtualKey.E,
                    VirtualKey.F, VirtualKey.G, VirtualKey.H, VirtualKey.I, VirtualKey.J,
                    VirtualKey.K, VirtualKey.L, VirtualKey.M, VirtualKey.N, VirtualKey.O,
                    VirtualKey.P, VirtualKey.Q, VirtualKey.R, VirtualKey.S, VirtualKey.T,
                    VirtualKey.U, VirtualKey.V, VirtualKey.W, VirtualKey.X, VirtualKey.Y, VirtualKey.Z,
                    VirtualKey.NUMPAD0, VirtualKey.NUMPAD1, VirtualKey.NUMPAD2, VirtualKey.NUMPAD3,
                    VirtualKey.NUMPAD4, VirtualKey.NUMPAD5, VirtualKey.NUMPAD6, VirtualKey.NUMPAD7,
                    VirtualKey.NUMPAD8, VirtualKey.NUMPAD9,
                    VirtualKey.SPACE, VirtualKey.RETURN, VirtualKey.TAB,
                    VirtualKey.INSERT, VirtualKey.DELETE, VirtualKey.HOME, VirtualKey.END,
                    VirtualKey.PRIOR, VirtualKey.NEXT,
                    VirtualKey.UP, VirtualKey.DOWN, VirtualKey.LEFT, VirtualKey.RIGHT,
                    VirtualKey.MULTIPLY, VirtualKey.ADD, VirtualKey.SUBTRACT, VirtualKey.DIVIDE, VirtualKey.DECIMAL,
                    VirtualKey.OEM_1, VirtualKey.OEM_PLUS, VirtualKey.OEM_COMMA, VirtualKey.OEM_MINUS,
                    VirtualKey.OEM_PERIOD, VirtualKey.OEM_2, VirtualKey.OEM_3, VirtualKey.OEM_4,
                    VirtualKey.OEM_5, VirtualKey.OEM_6, VirtualKey.OEM_7, VirtualKey.OEM_8, VirtualKey.OEM_102
                };

                foreach (var vk in keysToCheck)
                {
                    if (keyState[vk])
                    {
                        int keyCode = (int)vk;
                        Plugin.PluginLog.Info($"Captured keybind: {vk}");
                        onKeybindSet(keyCode, ctrl, alt, shift);
                        capturing = false;
                        mouseCaptureReadyAt = null;
                        break;
                    }
                }
            }
        }
    }

    private string FormatKeybind(int key, bool ctrl, bool alt, bool shift)
    {
        if (key == 0)
            return "Not set";

        string result = "";
        if (ctrl) result += "Ctrl + ";
        if (alt) result += "Alt + ";
        if (shift) result += "Shift + ";
        result += ((VirtualKey)key).ToString();

        return result;
    }
}
