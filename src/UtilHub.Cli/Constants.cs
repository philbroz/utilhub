internal static class Constants
{
    // Application
    public const string AppName = "UTIL HUB";
    public const string AppSubtitle = "Launcher for file-based utilities (.NET 10)";
    public const string DefaultUtilitiesPath = "Utilities";

    // Folders and Files
    public const string UtilitiesFolderName = "Utilities";
    public const string FilePattern = "*.csx";

    // Actions
    public const string ActionRun = "Run";
    public const string ActionReloadList = "Reload List";
    public const string ActionOpenUtilitiesFolder = "Open Utilities Folder";
    public const string ActionExit = "Exit";

    // UI Headers
    public const string HeaderAction = "Action";
    public const string HeaderUtilities = "Utilities";

    // UI Labels
    public const string LabelId = "Id:";
    public const string LabelFile = "File:";
    public const string LabelDescription = "Description:";
    public const string LabelTags = "Tags:";

    // Status Messages
    public const string StatusLoading = "Loading utilities...";
    public const string StatusBuilding = "Building...";
    public const string StatusRunning = "Running:";

    // Keyboard Hints
    public const string KeyboardHints = "↑/↓ move • PgUp/PgDn • Enter select • Esc exit";

    // Messages
    public const string MessageFolderNotFound = "Folder not found:";
    public const string MessageNoUtilities = "No .cs utilities found in /Utilities";
    public const string MessagePressKeyToExit = "Press any key to exit...";
    public const string MessagePressKeyToReturn = "Press any key to return to menu...";
    public const string MessageDone = "Done.";
    public const string MessageFinishedWithExitCode = "Finished with exit code:";
    public const string MessageExitCode = "Exit code:";
    public const string MessageBuildFailed = "Build failed";
    public const string MessageFailedToStart = "Failed to start process.";
    public const string MessageCouldNotOpenFolder = "Could not open folder automatically. Path:";

    // Formatting
    public const string FormatItemCount = "({0}/{1})";
    public const string FormatExitCode = "(exit code {0})";

    // Colors (Markup)
    public const string ColorRed = "red";
    public const string ColorYellow = "yellow";
    public const string ColorGreen = "green";
    public const string ColorGrey = "grey";
    public const string ColorAqua = "aqua";
    public const string ColorBlue = "blue";
    public const string ColorCyan = "cyan";
    public const string ColorWhite = "white";

    // Styles (Markup)
    public const string StyleBold = "bold";

    // Default Values
    public const string DefaultPlaceholder = "-";
    public const string DefaultIconEmoji = "🔹";
    public const string DefaultIconText = "•";

    // Environment Variables
    public const string EnvNoEmoji = "UTILHUB_NO_EMOJI";

    // Icon Keywords
    internal const string KeywordDb = "db";
    internal const string KeywordSql = "sql";
    internal const string KeywordBackup = "backup";
    internal const string KeywordMedia = "media";
    internal const string KeywordVideo = "video";
    internal const string KeywordAudio = "audio";
    internal const string KeywordConvert = "convert";
    internal const string KeywordNet = "net";
    internal const string KeywordHttp = "http";
    internal const string KeywordApi = "api";
    internal const string KeywordGcp = "gcp";
    internal const string KeywordAzure = "azure";
    internal const string KeywordDev = "dev";
    internal const string KeywordBuild = "build";
    internal const string KeywordCi = "ci";
    internal const string KeywordTest = "test";
    internal const string KeywordFile = "file";
    internal const string KeywordFs = "fs";
    internal const string KeywordIo = "io";

    // Icons
    internal const string IconDatabase = "🗄️";
    internal const string IconMedia = "🎬";
    internal const string IconNetwork = "🌐";
    internal const string IconDevelopment = "🔧";
    internal const string IconFile = "📁";
}
