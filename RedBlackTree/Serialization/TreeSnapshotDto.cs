using System.Text.Json.Serialization;
using RedBlackTree.Models;

namespace RedBlackTree.Serialization;

public sealed class TreeSnapshotDto
{
    [JsonPropertyName("Tree")]
    public TreeNodeModel? Tree { get; set; }

    [JsonPropertyName("NewNodeValue")]
    public string FocusValue { get; set; } = string.Empty;

    [JsonPropertyName("_nodeIdCounter")]
    public int NodeIdCounter { get; set; }

    [JsonPropertyName("Quantity")]
    public int Count { get; set; }

    [JsonPropertyName("isSearchClicked")]
    public bool IsSearchHighlighted { get; set; }
}
