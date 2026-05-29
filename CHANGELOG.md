# Changelog

All notable changes to Mount Select will be documented in this file.

## [1.0.0] - 2026-05-29

### Added
- Initial release of Mount Select
- Job-specific mount rotations - create custom mount pools for each job/class
- Visual mount selection window with searchable icon grid
- Multi-seat mount favorites system for group content
- Customizable keybinds for quick mount summoning
- Auto job detection when opening mount selection window
- Chat commands: `/mountselect`, `/mountconfig`, `/qmount`, `/multimount`

### Fixed
- PvP mounting now works correctly - plugin no longer blocks mount commands when InCombat flag is active in PvP content
- Mount commands now respond as quickly as native game mounts in PvP scenarios

### Known Limitations
- Keybinds may trigger while typing in chat (API limitation)
- Keybinds are disabled when game window is not focused (intentional)
