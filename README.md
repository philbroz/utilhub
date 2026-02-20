# utilhub

UtilHub is a small terminal launcher for discovering and running single-file .NET utilities. It scans a configured utilities folder, extracts metadata, and presents an interactive console UI (using Spectre.Console) so you can preview, build, run, or open utilities with live output and progress spinners.

Key goals
- Make it easy to organize and run single-file .NET tools from a developer utilities directory
- Provide fast metadata previews and live execution output in a friendly TUI
- Keep the tool lightweight and cross-platform via .NET

Prerequisites
- .NET 10 SDK installed (see https://dotnet.microsoft.com)
- A terminal that supports ANSI escape sequences (Windows Terminal, macOS Terminal, Linux terminals)

Quick start
1. Clone the repository:

   `git clone <repo-url>`

2. Build the CLI project:

   `dotnet build src/UtilHub.Cli`

3. Run the CLI locally:

   `dotnet run --project src/UtilHub.Cli -- [options]`

Basic usage
- The CLI scans a utilities folder (configurable) for single-file `.cs` or `.csx` utilities. It reads metadata from the files and displays a list with previews.
- From the interactive UI you can choose to:
  - Build a utility
  - Run a utility and stream output to the console
  - Open the utility in your editor

Configuration
- Default configuration and constants are defined in the `src/UtilHub.Cli` project. Adjust paths or defaults by editing the corresponding configuration or `Constants` class.

Development
- Tests (if any) and the CLI target .NET 10. Use the SDK commands to build and run projects.
- To debug in your IDE (Visual Studio / VS Code) open the solution and run the `UtilHub.Cli` startup project.

Contributing
- Bug reports, feature requests and pull requests are welcome. Please follow standard GitHub workflow: fork, feature branch, open a pull request with a clear description and relevant tests.

License
- This repository does not specify a license in the README. Add a `LICENSE` file to make the project's license explicit.

Troubleshooting
- If Git reports permission errors for Visual Studio caches (for example `.vs/`), add `.vs/` to `.gitignore` and remove tracked `.vs` files with `git rm --cached -r .vs` after closing Visual Studio or any process locking those files.

More
- See the `src` directory for the CLI and shared libraries. Inspect `Program.cs` in `src/UtilHub.Cli` for the entry point and `UtilHubMeta` types in `src/UtilHub.Shared` for metadata handling.
