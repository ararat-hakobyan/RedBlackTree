using RedBlackTree.Models;

namespace RedBlackTree.Recording;

public interface ITreeStepRecorder<T>
{
    bool IsEnabled { get; }

    IReadOnlyList<TreeOperationStep> Steps { get; }

    void Clear();

    void Record(
        StepAction action,
        Func<TreeNodeModel?> stateFactory,
        string? nodeId = null,
        string? nodeValue = null,
        NodeColor? color = null);
}
