namespace RedBlackTree.Services;

public readonly record struct OperationResult(bool Success, string? Message)
{
    public static OperationResult Ok(string? message = null) => new(true, message);

    public static OperationResult Fail(string? message = null) => new(false, message);

    public static readonly OperationResult Ignored = new(false, null);
}

public readonly record struct ExportResult(byte[]? Content, string? FileName, string? Error)
{
    public bool Success => Content is not null;

    public static ExportResult Ok(byte[] content, string fileName) => new(content, fileName, null);

    public static ExportResult Fail(string error) => new(null, null, error);
}
