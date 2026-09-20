using System.Text.Json.Serialization;

namespace RedBlackTree.Models;

public sealed class RBTreeNode<T>
{
    internal RBTreeNode(T value, string nodeId, NodeColor color = NodeColor.Red)
    {
        Value = value;
        NodeId = nodeId;
        Color = color;
    }

    public T Value { get; internal set; }

    public string NodeId { get; }

    public NodeColor Color { get; internal set; }

    [JsonIgnore]
    public RBTreeNode<T> Left { get; internal set; } = null!;

    [JsonIgnore]
    public RBTreeNode<T> Right { get; internal set; } = null!;

    [JsonIgnore]
    public RBTreeNode<T> Parent { get; internal set; } = null!;
}
