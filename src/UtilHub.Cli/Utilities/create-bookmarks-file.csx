#!/usr/bin/env dotnet
// @id create-bookmarks-file
// @title Create Bookmarks HTML File from TSV/CSV
// @desc Creates a bookmarks HTML file from a TSV/CSV list of application links, grouped by environment.
// @tags bookmarks,html,tsv,csv,utilities
// @author Filippo Frater

using System;
using System.Text;
using System.Net;

// Usage:
//  dotnet run .\CreateBookmarksFile.cs -- --input links.tsv --output bookmarks.html --root "Nebula"
// File Format:
//  Application Name	Environment	URL
//  FantasyAppName ITA https://myFantasyUrl
// Input formats supported:
// - TSV (default): columns separated by TAB
// - CSV: use --delimiter "," (also supports ';' etc.)

var argsDict = ParseArgs(args);

var inputPath = GetArg(argsDict, "--input") ?? "links.tsv";
var outputPath = GetArg(argsDict, "--output") ?? "bookmarks.html";
var rootFolder = GetArg(argsDict, "--root") ?? "Imported";
var delimiter = GetArg(argsDict, "--delimiter") ?? "\t";

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    Environment.Exit(1);
}

var lines = File.ReadAllLines(inputPath);
if (lines.Length < 2)
{
    Console.Error.WriteLine("Input file must contain a header row and at least one data row.");
    Environment.Exit(1);
}

var header = SplitLine(lines[0], delimiter);
var idxApp = FindColumnIndex(header, "Application Name", "App", "Application");
var idxEnv = FindColumnIndex(header, "Environment", "Env");
var idxUrl = FindColumnIndex(header, "URL", "Url", "Link");

if (idxApp < 0 || idxEnv < 0 || idxUrl < 0)
{
    Console.Error.WriteLine("Missing required columns. Expected: Application Name, Environment, URL");
    Console.Error.WriteLine($"Found headers: {string.Join(" | ", header)}");
    Environment.Exit(1);
}

var rows = new List<Row>();

for (int i = 1; i < lines.Length; i++)
{
    if (string.IsNullOrWhiteSpace(lines[i])) continue;

    var parts = SplitLine(lines[i], delimiter);

    // Allow short/irregular lines without crashing; skip incomplete rows.
    if (parts.Length <= Math.Max(idxUrl, Math.Max(idxApp, idxEnv)))
        continue;

    var app = parts[idxApp].Trim();
    var env = parts[idxEnv].Trim();
    var url = parts[idxUrl].Trim();

    if (string.IsNullOrWhiteSpace(url)) continue;
    if (string.IsNullOrWhiteSpace(app)) app = url; // Fallback title
    if (string.IsNullOrWhiteSpace(env)) env = "Unknown";

    rows.Add(new Row(app, env, url));
}

// Grouping: Root -> Environment folders -> links
var grouped = rows
    .GroupBy(r => r.Environment, StringComparer.OrdinalIgnoreCase)
    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
    .ToList();

var html = BuildBookmarksHtml(rootFolder, grouped);

File.WriteAllText(outputPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
Console.WriteLine($"Generated: {outputPath}");

static string BuildBookmarksHtml(string rootFolder, List<IGrouping<string, Row>> envGroups)
{
    var sb = new StringBuilder();

    sb.AppendLine("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
    sb.AppendLine("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
    sb.AppendLine("<TITLE>Bookmarks</TITLE>");
    sb.AppendLine("<H1>Bookmarks</H1>");
    sb.AppendLine("<DL><p>");

    // Root folder
    sb.AppendLine($"  <DT><H3>{HtmlEncode(rootFolder)}</H3>");
    sb.AppendLine("  <DL><p>");

    foreach (var envGroup in envGroups)
    {
        sb.AppendLine($"    <DT><H3>{HtmlEncode(envGroup.Key)}</H3>");
        sb.AppendLine("    <DL><p>");

        // De-duplicate within env (same URL), keep stable order by app name then url.
        var links = envGroup
            .GroupBy(r => r.Url, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(r => r.ApplicationName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Url, StringComparer.OrdinalIgnoreCase);

        foreach (var r in links)
        {
            sb.AppendLine($"      <DT><A HREF=\"{HtmlAttributeEncode(r.Url)}\">{HtmlEncode(r.ApplicationName)}</A>");
        }

        sb.AppendLine("    </DL><p>");
    }

    sb.AppendLine("  </DL><p>");
    sb.AppendLine("</DL><p>");

    return sb.ToString();
}

static string HtmlEncode(string s) => WebUtility.HtmlEncode(s);

static string HtmlAttributeEncode(string s)
{
    // Minimal attribute escaping; WebUtility.HtmlEncode is ok, but we also guard quotes explicitly.
    var encoded = WebUtility.HtmlEncode(s);
    return encoded.Replace("\"", "&quot;");
}

static string[] SplitLine(string line, string delimiter)
{
    // This is intentionally simple: good for TSV and simple CSV without quoted delimiters.
    // If you need full CSV quoting rules, tell me and I’ll switch to a robust parser.
    return line.Split(delimiter, StringSplitOptions.None);
}

static int FindColumnIndex(string[] headers, params string[] candidates)
{
    for (int i = 0; i < headers.Length; i++)
    {
        var h = headers[i].Trim();
        foreach (var c in candidates)
        {
            if (h.Equals(c, StringComparison.OrdinalIgnoreCase))
                return i;
        }
    }
    return -1;
}

static Dictionary<string, string> ParseArgs(string[] args)
{
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    for (int i = 0; i < args.Length; i++)
    {
        var key = args[i];
        if (!key.StartsWith("--")) continue;

        var value = (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            ? args[i + 1]
            : "true";

        dict[key] = value;
    }

    return dict;
}

static string? GetArg(Dictionary<string, string> dict, string key)
    => dict.TryGetValue(key, out var v) ? v : null;

record Row(string ApplicationName, string Environment, string Url);
