using System.Globalization;

namespace RedBlackTree.Resources;

public static class Messages
{
    public const string EmptyTree = "The tree is empty.";
    public const string FileNotSelected = "No file selected.";
    public const string OnlyJsonAllowed = "Only JSON files are allowed.";
    public const string FileIsEmpty = "The file is empty.";
    public const string FileTooLarge = "The file is too large. The maximum size is {0} MB.";
    public const string InvalidJson = "Invalid JSON format.";
    public const string ImportFailed = "The file could not be imported.";

    public static string Removed(string value) => $"{value} has been removed.";

    public static string NotFound(string value) => $"{value} was not found.";

    public static string Found(string value) => $"{value} was found.";

    public static string Statistics(int count, string minimum, string maximum, int height) =>
        $"Nodes: {count}, " +
        $"smallest: {minimum}, " +
        $"largest: {maximum}, " +
        $"height: {height}.";

    public static string TooLarge(long maxBytes) =>
        string.Format(CultureInfo.InvariantCulture, FileTooLarge, maxBytes / (1024 * 1024));
}
