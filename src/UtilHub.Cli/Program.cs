using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;
using static UtilHubMeta;

internal static class Program
{
    const string AppName = Constants.AppName;

    public static int Main(string[] args)
    {
        // Set console encoding to UTF-8 to properly display emoji icons
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var defaultUtilitiesFolder = Path.Combine(Environment.CurrentDirectory, Constants.DefaultUtilitiesPath);
        const int headerLinesToScan = 60;

        // load utiilities from default folder, if missing show message and exit (no point in running without utilities)
        if (!Directory.Exists(defaultUtilitiesFolder))
        {
            AnsiConsole.MarkupLine($"[{Constants.ColorRed}]{Constants.MessageFolderNotFound}[/] {Escape(defaultUtilitiesFolder)}");
            return 1;
        }

        var utilitiesFromArgs = String.Empty;

        // append args folder to default folder, so we can load utilities from both default and args folder
        if (args.Length > 0)
        {
            var argFolder = args[0];
            if (Directory.Exists(argFolder))
                utilitiesFromArgs = argFolder;
        }

        while (true)
        {
            AnsiConsole.Clear();
            RenderHeader();

            var utilities = LoadUtilities(defaultUtilitiesFolder, headerLinesToScan);
            utilities.AddRange(LoadUtilities(utilitiesFromArgs, headerLinesToScan));
            if (utilities.Count == 0)
            {
                AnsiConsole.MarkupLine($"[{Constants.ColorYellow}]{Constants.MessageNoUtilities}[/]");
                AnsiConsole.MarkupLine($"[{Constants.ColorGrey}]{Constants.MessagePressKeyToExit}[/]");
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
                    .Title($"[{Constants.StyleBold}]{Constants.HeaderAction}[/]")
                    .PageSize(8)
                    .AddChoices(Constants.ActionRun, Constants.ActionReloadList, Constants.ActionOpenUtilitiesFolder, Constants.ActionExit)
            );

            if (action == Constants.ActionExit)
                return 0;

            if (action == Constants.ActionReloadList)
                continue;

            if (action == Constants.ActionOpenUtilitiesFolder)
            {
                OpenFolder(defaultUtilitiesFolder);
                AnsiConsole.MarkupLine($"[{Constants.ColorGrey}]{Constants.MessagePressKeyToReturn}[/]");
                Console.ReadKey(true);
                continue;
            }

            if (action == Constants.ActionRun)
            {
                var exitCode = RunUtilityWithSpinner(selected.FilePath);

                AnsiConsole.WriteLine();
                if (exitCode == 0)
                    AnsiConsole.MarkupLine($"[{Constants.ColorGreen}]{Constants.MessageDone}[/]");
                else
                    AnsiConsole.MarkupLine($"[{Constants.ColorYellow}]{Constants.MessageFinishedWithExitCode}[/] [{Constants.StyleBold}]{exitCode}[/]");

                AnsiConsole.MarkupLine($"[{Constants.ColorGrey}]{Constants.MessagePressKeyToReturn}[/]");
                Console.ReadKey(true);
            }
        }
    }

    // ---------------- Loading ----------------

    private static List<UtilityInfo> LoadUtilities(string folder, int headerLinesToScan)
    {
        // Guard: if folder is not provided or doesn't exist, return empty list
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return new List<UtilityInfo>();

        return AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(Constants.ColorAqua))
            .Start(Constants.StatusLoading, _ =>
            {
                var files = Directory.GetFiles(folder, Constants.FilePattern, SearchOption.TopDirectoryOnly)
                    .OrderBy(Path.GetFileName)
                    .ToList();

                var list = files
                    .Select(f => UtilHubMeta.ReadInfo(f, headerLinesToScan))
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

        AnsiConsole.MarkupLine($"[{Constants.ColorGrey}]{Constants.KeyboardHints}[/]");
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

            var pointer = isSelected ? $"[{Constants.StyleBold} {Constants.ColorAqua}]>[/] " : "  ";
            var icon = Escape(PickIcon(u));
            var title = isSelected
                ? $"[{Constants.StyleBold} {Constants.ColorWhite}]{Escape(u.Title)}[/]"
                : $"[{Constants.ColorGrey}]{Escape(u.Title)}[/]";

            // Compact: icon + title only
            lines.Add(new Markup($"{pointer}{icon} {title}"));
        }

        lines.Add(new Rule().RuleStyle(Constants.ColorGrey));
        lines.Add(new Markup($"[{Constants.ColorGrey}]{string.Format(Constants.FormatItemCount, index + 1, utilities.Count)}[/]"));

        return new Panel(new Rows(lines))
        {
            Header = new PanelHeader($"[{Constants.StyleBold} {Constants.ColorCyan}]{Constants.HeaderUtilities}[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1, 1, 1)
        }.Expand();
    }

    private static Panel BuildRightPreviewPanel(UtilityInfo u)
    {
        var desc = string.IsNullOrWhiteSpace(u.Description) ? Constants.DefaultPlaceholder : u.Description;
        var tags = u.Tags.Length == 0 ? Constants.DefaultPlaceholder : string.Join(", ", u.Tags);

        var content = new Rows(
            new Markup($"[{Constants.StyleBold}]{Constants.LabelId}[/] {Escape(u.Id)}"),
            new Markup($"[{Constants.StyleBold}]{Constants.LabelFile}[/] {Escape(Path.GetFileName(u.FilePath))}"),
            new Markup($"[{Constants.StyleBold}]{Constants.LabelDescription}[/] {Escape(desc)}"),
            new Markup($"[{Constants.StyleBold}]{Constants.LabelTags}[/] {Escape(tags)}")
        );

        return new Panel(content)
        {
            Header = new PanelHeader($"[{Constants.StyleBold} {Constants.ColorAqua}]{Escape(PickIcon(u))} {Escape(u.Title)}[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1, 1, 1)
        }.Expand();
    }

    // ---------------- Header ----------------

    private static void RenderHeader()
    {
        AnsiConsole.Write(new Rule($"[{Constants.StyleBold} {Constants.ColorBlue}]{AppName}[/]").RuleStyle(Constants.ColorBlue).Centered());

        AnsiConsole.Write(
            new FigletText(AppName)
                .Centered()
                .Color(Color.Aqua)
        );

        AnsiConsole.MarkupLine($"[{Constants.ColorGrey}]{Constants.AppSubtitle}[/]");
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
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Constants.EnvNoEmoji)))
            return Constants.DefaultIconText;

        var name = (u.Id + " " + u.Title + " " + Path.GetFileName(u.FilePath)).ToLowerInvariant();
        var tags = u.Tags.Select(t => t.ToLowerInvariant()).ToArray();

        bool Has(string s) => name.Contains(s) || tags.Contains(s);

        if (Has(Constants.KeywordDb) || Has(Constants.KeywordSql) || Has(Constants.KeywordBackup))
            return Constants.IconDatabase;
        if (Has(Constants.KeywordMedia) || Has(Constants.KeywordVideo) || Has(Constants.KeywordAudio) || Has(Constants.KeywordConvert))
            return Constants.IconMedia;
        if (Has(Constants.KeywordNet) || Has(Constants.KeywordHttp) || Has(Constants.KeywordApi) || Has(Constants.KeywordGcp) || Has(Constants.KeywordAzure))
            return Constants.IconNetwork;
        if (Has(Constants.KeywordDev) || Has(Constants.KeywordBuild) || Has(Constants.KeywordCi) || Has(Constants.KeywordTest))
            return Constants.IconDevelopment;
        if (Has(Constants.KeywordFile) || Has(Constants.KeywordFs) || Has(Constants.KeywordIo))
            return Constants.IconFile;

        return Constants.DefaultIconEmoji;
    }

    // ---------------- Run utility (spinner) ----------------

    private static int RunUtilityWithSpinner(string filePath)
    {
        // 1) Build step with spinner (quiet)
        var buildExit = AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(Constants.ColorAqua))
            .Start(Constants.StatusBuilding, _ => BuildUtilityQuiet(filePath));

        if (buildExit != 0)
        {
            AnsiConsole.MarkupLine($"[{Constants.ColorRed}]{Constants.MessageBuildFailed}[/] {string.Format(Constants.FormatExitCode, buildExit)}.");
            return buildExit;
        }

        // 2) Run step WITHOUT spinner so the utility output is visible live
        AnsiConsole.MarkupLine($"[{Constants.ColorGrey}]{Constants.StatusRunning}[/] [{Constants.StyleBold}]{Markup.Escape(Path.GetFileName(filePath))}[/]\n");
        var runExit = RunUtilityLive(filePath);

        if (runExit != 0)
            AnsiConsole.MarkupLine($"\n[{Constants.ColorYellow}]{Constants.MessageExitCode}[/] [{Constants.StyleBold}]{runExit}[/]");

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
            AnsiConsole.MarkupLine($"[{Constants.ColorRed}]{Constants.MessageFailedToStart}[/]");
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
            AnsiConsole.MarkupLine($"[{Constants.ColorYellow}]{Constants.MessageCouldNotOpenFolder}[/] {Escape(folderPath)}");
        }
    }

    private static string Escape(string s) => Markup.Escape(s ?? "");
}

