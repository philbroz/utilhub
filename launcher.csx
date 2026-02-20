#!/usr/bin/env dotnet
#:package Spectre.Console@0.49.1
#load "Strings.csx"

using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;

internal record UtilityInfo(
    string Id,
    string Title,
    string Description,
    string[] Tags,
    string FilePath
);

internal static class Program
{
    const string AppName = Strings.AppName;

    public static int Main()
    {
        var utilitiesFolder = Path.Combine(Environment.CurrentDirectory, "Utilities");
        const int headerLinesToScan = 60;

        if (!Directory.Exists(utilitiesFolder))
        {
            AnsiConsole.MarkupLine($"[{Strings.ColorRed}]{Strings.MessageFolderNotFound}[/] {Escape(utilitiesFolder)}");
            return 1;
        }

        while (true)
        {
            AnsiConsole.Clear();
            RenderHeader();

            var utilities = LoadUtilities(utilitiesFolder, headerLinesToScan);
            if (utilities.Count == 0)
            {
                AnsiConsole.MarkupLine($"[{Strings.ColorYellow}]{Strings.MessageNoUtilities}[/]");
                AnsiConsole.MarkupLine($"[{Strings.ColorGrey}]{Strings.MessagePressKeyToExit}[/]");
                Console.ReadKey(true);
                return 0;
            }

            var selected = InteractivePicker(utilities);
            if (selected is null)
                return 0;

            // Action menu
            AnsiConsole.Clear();
            RenderHeader();
            ShowPreview(selected);

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[{Strings.StyleBold}]{Strings.HeaderAction}[/]")
                    .PageSize(8)
                    .AddChoices(Strings.ActionRun, Strings.ActionReloadList, Strings.ActionOpenUtilitiesFolder, Strings.ActionExit)
            );

            if (action == Strings.ActionExit)
                return 0;

            if (action == Strings.ActionReloadList)
                continue;

            if (action == Strings.ActionOpenUtilitiesFolder)
            {
                OpenFolder(utilitiesFolder);
                AnsiConsole.MarkupLine($"[{Strings.ColorGrey}]{Strings.MessagePressKeyToReturn}[/]");
                Console.ReadKey(true);
                continue;
            }

            if (action == Strings.ActionRun)
            {
                var exitCode = RunUtilityWithSpinner(selected.FilePath);

                AnsiConsole.WriteLine();
                if (exitCode == 0)
                    AnsiConsole.MarkupLine($"[{Strings.ColorGreen}]{Strings.MessageDone}[/]");
                else
                    AnsiConsole.MarkupLine($"[{Strings.ColorYellow}]{Strings.MessageFinishedWithExitCode}[/] [{Strings.StyleBold}]{exitCode}[/]");

                AnsiConsole.MarkupLine($"[{Strings.ColorGrey}]{Strings.MessagePressKeyToReturn}[/]");
                Console.ReadKey(true);
            }
        }
    }

    // ---------------- Loading ----------------

    private static List<UtilityInfo> LoadUtilities(string folder, int headerLinesToScan)
    {
        return AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(Strings.ColorAqua))
            .Start(Strings.StatusLoading, _ =>
            {
                var files = Directory.GetFiles(folder, Strings.FilePattern, SearchOption.TopDirectoryOnly)
                    .OrderBy(Path.GetFileName)
                    .ToList();

                var list = files
                    .Select(f => ReadInfo(f, headerLinesToScan))
                    .OrderBy(u => u.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return list;
            });
    }

    // ---------------- Picker UI (two panes) ----------------

    private static UtilityInfo? InteractivePicker(List<UtilityInfo> utilities)
    {
        var index = 0;
        var scroll = 0;
        var maxVisible = Math.Clamp(utilities.Count, 6, 12);

        AnsiConsole.MarkupLine($"[{Strings.ColorGrey}]{Strings.KeyboardHints}[/]");
        AnsiConsole.WriteLine();

        UtilityInfo? selected = null;

        AnsiConsole.Live(RenderTwoPane(utilities, index, scroll, maxVisible))
            .AutoClear(false)
            .Overflow(VerticalOverflow.Crop)
            .Start(ctx =>
            {
                while (true)
                {
                    var key = AnsiConsole.Console.Input.ReadKey(true);
                    if (key is null) break;

                    if (key.Value.Key == ConsoleKey.Escape)
                    {
                        selected = null;
                        break;
                    }

                    if (key.Value.Key == ConsoleKey.Enter)
                    {
                        selected = utilities[index];
                        break;
                    }

                    if (key.Value.Key == ConsoleKey.UpArrow) index = Math.Max(0, index - 1);
                    else if (key.Value.Key == ConsoleKey.DownArrow) index = Math.Min(utilities.Count - 1, index + 1);
                    else if (key.Value.Key == ConsoleKey.PageUp) index = Math.Max(0, index - maxVisible);
                    else if (key.Value.Key == ConsoleKey.PageDown) index = Math.Min(utilities.Count - 1, index + maxVisible);

                    if (index < scroll) scroll = index;
                    if (index >= scroll + maxVisible) scroll = index - maxVisible + 1;

                    ctx.UpdateTarget(RenderTwoPane(utilities, index, scroll, maxVisible));
                }
            });

        return selected;
    }

    private static IRenderable RenderTwoPane(List<UtilityInfo> utilities, int index, int scroll, int maxVisible)
    {
        var left = BuildLeftListPanel(utilities, index, scroll, maxVisible);
        var right = BuildRightPreviewPanel(utilities[index]);

        return new Columns(left, right) { Expand = true };
    }

    private static Panel BuildLeftListPanel(List<UtilityInfo> utilities, int index, int scroll, int maxVisible)
    {
        var lines = new List<IRenderable>();

        var end = Math.Min(utilities.Count, scroll + maxVisible);
        for (var i = scroll; i < end; i++)
        {
            var u = utilities[i];
            var isSelected = i == index;

            var pointer = isSelected ? $"[{Strings.StyleBold} {Strings.ColorAqua}]>[/] " : "  ";
            var icon = Escape(PickIcon(u));
            var title = isSelected
                ? $"[{Strings.StyleBold} {Strings.ColorWhite}]{Escape(u.Title)}[/]"
                : $"[{Strings.ColorGrey}]{Escape(u.Title)}[/]";

            // Compact: icon + title only
            lines.Add(new Markup($"{pointer}{icon} {title}"));
        }

        lines.Add(new Rule().RuleStyle(Strings.ColorGrey));
        lines.Add(new Markup($"[{Strings.ColorGrey}]{string.Format(Strings.FormatItemCount, index + 1, utilities.Count)}[/]"));

        return new Panel(new Rows(lines))
        {
            Header = new PanelHeader($"[{Strings.StyleBold} {Strings.ColorCyan}]{Strings.HeaderUtilities}[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1, 1, 1)
        }.Expand();
    }

    private static Panel BuildRightPreviewPanel(UtilityInfo u)
    {
        var desc = string.IsNullOrWhiteSpace(u.Description) ? Strings.DefaultPlaceholder : u.Description;
        var tags = u.Tags.Length == 0 ? Strings.DefaultPlaceholder : string.Join(", ", u.Tags);

        var content = new Rows(
            new Markup($"[{Strings.StyleBold}]{Strings.LabelId}[/] {Escape(u.Id)}"),
            new Markup($"[{Strings.StyleBold}]{Strings.LabelFile}[/] {Escape(Path.GetFileName(u.FilePath))}"),
            new Markup($"[{Strings.StyleBold}]{Strings.LabelDescription}[/] {Escape(desc)}"),
            new Markup($"[{Strings.StyleBold}]{Strings.LabelTags}[/] {Escape(tags)}")
        );

        return new Panel(content)
        {
            Header = new PanelHeader($"[{Strings.StyleBold} {Strings.ColorAqua}]{Escape(PickIcon(u))} {Escape(u.Title)}[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1, 1, 1)
        }.Expand();
    }

    // ---------------- Header ----------------

    private static void RenderHeader()
    {
        AnsiConsole.Write(new Rule($"[{Strings.StyleBold} {Strings.ColorBlue}]{AppName}[/]").RuleStyle(Strings.ColorBlue).Centered());

        AnsiConsole.Write(
            new FigletText(AppName)
                .Centered()
                .Color(Color.Aqua)
        );

        AnsiConsole.MarkupLine($"[{Strings.ColorGrey}]{Strings.AppSubtitle}[/]");
        AnsiConsole.WriteLine();
    }

    private static void ShowPreview(UtilityInfo u)
    {
        AnsiConsole.Write(BuildRightPreviewPanel(u));
        AnsiConsole.WriteLine();
    }

    // ---------------- Icons ----------------
    // Emoji may depend on terminal/font. If you want to disable them:
    // set environment variable UTILHUB_NO_EMOJI=1
    private static string PickIcon(UtilityInfo u)
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Strings.EnvNoEmoji)))
            return Strings.DefaultIconText;

        var name = (u.Id + " " + u.Title + " " + Path.GetFileName(u.FilePath)).ToLowerInvariant();
        var tags = u.Tags.Select(t => t.ToLowerInvariant()).ToArray();

        bool Has(string s) => name.Contains(s) || tags.Contains(s);

        if (Has("db") || Has("sql") || Has("backup")) return "🗄️";
        if (Has("media") || Has("video") || Has("audio") || Has("convert")) return "🎞️";
        if (Has("net") || Has("http") || Has("api") || Has("gcp") || Has("azure")) return "🌐";
        if (Has("dev") || Has("build") || Has("ci") || Has("test")) return "⚙️";
        if (Has("file") || Has("fs") || Has("io")) return "📁";

        return Strings.DefaultIconEmoji;
    }

    // ---------------- Metadata parsing ----------------

    private static UtilityInfo ReadInfo(string filePath, int maxLines)
    {
        var defaultId = Path.GetFileNameWithoutExtension(filePath);

        var id = defaultId;
        var title = defaultId;
        var desc = "";
        var tags = Array.Empty<string>();

        foreach (var raw in File.ReadLines(filePath).Take(maxLines))
        {
            var line = raw.Trim();
            if (!line.StartsWith("// @", StringComparison.Ordinal)) continue;

            if (TryGetValue(line, "@id", out var v1)) id = v1;
            else if (TryGetValue(line, "@title", out var v2)) title = v2;
            else if (TryGetValue(line, "@desc", out var v3)) desc = v3;
            else if (TryGetValue(line, "@tags", out var v4))
                tags = v4.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return new UtilityInfo(id, title, desc, tags, filePath);
    }

    private static bool TryGetValue(string line, string key, out string value)
    {
        // Expected format: "// @key value..."
        value = "";
        var prefix = $"// {key} ";
        if (!line.StartsWith(prefix, StringComparison.Ordinal)) return false;

        value = line[prefix.Length..].Trim();
        return true;
    }

    // ---------------- Run utility (spinner) ----------------

    private static int RunUtilityWithSpinner(string filePath)
    {
        // 1) Build step with spinner (quiet)
        var buildExit = AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(Strings.ColorAqua))
            .Start(Strings.StatusBuilding, _ => BuildUtilityQuiet(filePath));

        if (buildExit != 0)
        {
            AnsiConsole.MarkupLine($"[{Strings.ColorRed}]{Strings.MessageBuildFailed}[/] {string.Format(Strings.FormatExitCode, buildExit)}.");
            return buildExit;
        }

        // 2) Run step WITHOUT spinner so the utility output is visible live
        AnsiConsole.MarkupLine($"[{Strings.ColorGrey}]{Strings.StatusRunning}[/] [{Strings.StyleBold}]{Markup.Escape(Path.GetFileName(filePath))}[/]\n");
        var runExit = RunUtilityLive(filePath);

        if (runExit != 0)
            AnsiConsole.MarkupLine($"\n[{Strings.ColorYellow}]{Strings.MessageExitCode}[/] [{Strings.StyleBold}]{runExit}[/]");

        return runExit;
    }

    private static int BuildUtilityQuiet(string filePath)
    {
        // dotnet build for file-based apps (quiet output)
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build --file \"{filePath}\" -v:q",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi);
        if (p is null) return 1;

        // Consume output to avoid buffering issues (we keep it quiet)
        _ = p.StandardOutput.ReadToEnd();
        _ = p.StandardError.ReadToEnd();

        p.WaitForExit();
        return p.ExitCode;
    }

    private static int RunUtilityLive(string filePath)
    {
        // Run without --file build step, because we already built
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --file \"{filePath}\"",
            UseShellExecute = false
            // IMPORTANT: do not redirect output, so utility prints to console normally
        };
    
        using var p = Process.Start(psi);
        if (p is null)
        {
            AnsiConsole.MarkupLine($"[{Strings.ColorRed}]{Strings.MessageFailedToStart}[/]");
            return 1;
        }
    
        p.WaitForExit();
        return p.ExitCode;
    }

    private static void OpenFolder(string folderPath)
    {
        try
        {
            ProcessStartInfo psi =
                OperatingSystem.IsWindows()
                    ? new ProcessStartInfo("explorer", $"\"{folderPath}\"") { UseShellExecute = true }
                    : OperatingSystem.IsMacOS()
                        ? new ProcessStartInfo("open", $"\"{folderPath}\"") { UseShellExecute = false }
                        : new ProcessStartInfo("xdg-open", $"\"{folderPath}\"") { UseShellExecute = false };

            Process.Start(psi);
        }
        catch
        {
            AnsiConsole.MarkupLine($"[{Strings.ColorYellow}]{Strings.MessageCouldNotOpenFolder}[/] {Escape(folderPath)}");
        }
    }

    private static string Escape(string s) => Markup.Escape(s ?? "");
}
