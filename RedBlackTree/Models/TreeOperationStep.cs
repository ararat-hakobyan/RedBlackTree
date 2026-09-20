namespace RedBlackTree.Models;

public sealed class TreeOperationStep
{
    public StepAction Action { get; set; }

    public string? NodeValue { get; set; }

    public string? NodeId { get; set; }

    public string? Color { get; set; }

    public TreeNodeModel? TreeState { get; set; }
}
