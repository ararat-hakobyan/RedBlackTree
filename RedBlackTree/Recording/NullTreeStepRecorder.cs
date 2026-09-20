using RedBlackTree.Models;

namespace RedBlackTree.Recording;

public sealed class NullTreeStepRecorder<T> : ITreeStepRecorder<T>
{
    public static readonly NullTreeStepRecorder<T> Instance = new();

    private NullTreeStepRecorder()
    {
    }

    public bool IsEnabled => false;

    public IReadOnlyList<TreeOperationStep> Steps => Array.Empty<TreeOperationStep>();

    public void Clear()
    {
    }

    public void Record(
        StepAction action,
        Func<TreeNodeModel?> stateFactory,
        string? nodeId = null,
        string? nodeValue = null,
        NodeColor? color = null)
    {
    }
}
