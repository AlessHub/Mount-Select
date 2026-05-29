# FFXIV Dalamud Plugin Development Instructions

This workspace contains an FFXIV Dalamud plugin project using C# and .NET.

## Project Structure
- Plugin class with Dalamud API integration
- ImGui-based user interface
- Configuration system for persistent settings
- Command handling and chat integration
- Build configuration for Dalamud deployment

## Key Technologies
- **Dalamud** - FFXIV plugin framework
- **C# and .NET 8** - Primary development language
- **ImGui.NET** - User interface framework
- **Dalamud API** - Game integration and hooks

## Development Guidelines
- Follow Dalamud plugin conventions and best practices
- Use proper disposal patterns for resources
- Implement configuration persistence
- Handle plugin lifecycle events correctly
- Test thoroughly before deployment

## Build Process
- Target .NET 8 framework
- Use Dalamud NuGet packages
- Output to standard Dalamud plugin directory structure
- Include proper manifest and metadata files