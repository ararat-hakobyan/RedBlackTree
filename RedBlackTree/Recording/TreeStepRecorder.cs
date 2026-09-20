using RedBlackTree.Models;

namespace RedBlackTree.Recording;

public sealed class TreeStepRecorder<T> : ITreeStepRecorder<T>
{
    public const int MaxSteps = 500;

    private readonly List<TreeOperationStep> _steps = new();

    public bool IsEnabled => true;

    public IReadOnlyList<TreeOperationStep> Steps => _steps;

    public void Clear() => _steps.Clear();

    public void Record(
        StepAction action,
        Func<TreeNodeModel?> stateFactory,
        string? nodeId = null,
        string? nodeValue = null,
        NodeColor? color = null)
    {
        if (_steps.Count >= MaxSteps)
        {
            return;
        }

        _steps.Add(new TreeOperationStep
        {
            Action = action,
            NodeId = nodeId,
            NodeValue = nodeValue,
            Color = color?.ToString(),
            TreeState = stateFactory()
        });
    }

    public void Load(IEnumerable<TreeOperationStep> steps)
    {
        _steps.Clear();
        _steps.AddRange(steps.Take(MaxSteps));
    }
}
