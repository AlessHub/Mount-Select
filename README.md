# Mount Select - FFXIV Dalamud Plugin

Customize your mount experience! Set up job-specific mount rotations and summon them with a keybind or command. Includes multi-seat mount favorites and visual mount selection with icons.

## Features

- **Job-Specific Mount Rotations** - Create custom mount pools for each job/class, or use a default rotation for all
- **Visual Mount Selection** - Browse and select mounts with a searchable icon grid 
- **Multi-Seat Mount Favorites** - Maintain a separate list of multi-seat mounts for group content
- **Customizable Keybinds** - Set keybinds for quick mount summoning (both job mounts and multi-seat)
- **Auto Job Detection** - Automatically selects your current job when opening the mount selection window


### Commands

The plugin registers these chat commands:

- `/mountselect` - Opens the main plugin window
- `/mountconfig` - Opens the configuration window  
- `/qmount` - Summons a random mount assigned from your current class or uses the default assigned ones
- `/multimount` - Summons a random multi-seat from your selection


## Disclaimer

This plugin has been made with AI, vibe-coding, copilot, however you want to name it. This was my first attempt at vibe-coding and used it as an introduction to C#.
I have however led all of the decisions and read everything personally, made logs to check outputs and outcomes and overall "built" the plugin. I understand what it does and how it does it.

## Known Limitations

- **Mounting in PvP does not work entirely well** - I'm still currently working on this but I don't really like PvP that much so I'm just moving flags and hoping it works the next time I actually feel like doing frontline :(
- **Keybinds may trigger while typing in chat** - There's no reliable way to detect chat input in FFXIV via the Dalamud API. The plugin will display a warning when setting up keybinds. Consider using chat commands (`/qmount`, `/multimount`) or in-game macros as an alternative.
- **Keybinds are disabled when game window is not focused** - This is intentional to prevent accidental activation.

