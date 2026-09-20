namespace RedBlackTree.Models;

public sealed class TreeNodeModel
{
    public string NodeId { get; set; } = string.Empty;

    public string? Value { get; set; }

    public string Color { get; set; } = nameof(NodeColor.Black);

    public TreeNodeModel? Left { get; set; }

    public TreeNodeModel? Right { get; set; }
}
