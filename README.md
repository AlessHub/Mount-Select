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

## Known Limitations

- **Keybinds may trigger while typing in chat** - There's no reliable way to detect chat input in FFXIV via the Dalamud API. The plugin will display a warning when setting up keybinds. Consider using chat commands (`/qmount`, `/multimount`) or in-game macros as an alternative.
- **Keybinds are disabled when game window is not focused** - This is intentional to prevent accidental activation.

