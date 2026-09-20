using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedBlackTree.Models;

public sealed class TreeViewModel
{
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public required TreePayload Payload { get; init; }

    public string? Message { get; init; }

    public string PayloadJson => JsonSerializer.Serialize(Payload, PayloadOptions);
}

public sealed class TreePayload
{
    public TreeNodeModel? Root { get; init; }

    public IReadOnlyList<TreeOperationStep> Steps { get; init; } = Array.Empty<TreeOperationStep>();

    public string FocusValue { get; init; } = string.Empty;

    public int Count { get; init; }

    public bool IsSearchHighlighted { get; init; }

    public int AutoFocusThreshold { get; init; }
}
