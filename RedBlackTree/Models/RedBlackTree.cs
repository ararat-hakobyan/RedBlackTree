using RedBlackTree.Recording;

namespace RedBlackTree.Models;

public sealed class RedBlackTree<T>
{
    private readonly IComparer<T> _comparer;
    private readonly RBTreeNode<T> _nil;

    private ITreeStepRecorder<T> _recorder = NullTreeStepRecorder<T>.Instance;
    private int _nodeIdCounter;

    public RedBlackTree(IComparer<T> comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

        _nil = new RBTreeNode<T>(default!, NilNodeId, NodeColor.Black);
        _nil.Left = _nil;
        _nil.Right = _nil;
        _nil.Parent = _nil;

        Root = _nil;
    }

    public const string NilNodeId = "NIL";

    public RBTreeNode<T> Nil => _nil;

    public RBTreeNode<T> Root { get; private set; }

    public int Count { get; private set; }

    public const int AutoFocusThreshold = 14;

    public string FocusValue { get; private set; } = string.Empty;

    public bool IsSearchHighlighted { get; private set; }

    public bool IsEmpty => ReferenceEquals(Root, _nil);

    public IReadOnlyList<TreeOperationStep> Steps => _recorder.Steps;

    public void UseRecorder(ITreeStepRecorder<T> recorder)
    {
        _recorder = recorder ?? NullTreeStepRecorder<T>.Instance;
    }

    public RBTreeNode<T> Insert(T value)
    {
        _recorder.Clear();
        ResetNilLinks();

        IsSearchHighlighted = false;
        Count++;
        FocusValue = Count > AutoFocusThreshold ? Describe(value) ?? string.Empty : string.Empty;

        var inserted = new RBTreeNode<T>(value, NextNodeId())
        {
            Left = _nil,
            Right = _nil,
            Parent = _nil
        };

        var parent = _nil;
        var current = Root;

        while (!ReferenceEquals(current, _nil))
        {
            parent = current;
            current = _comparer.Compare(value, current.Value) < 0 ? current.Left : current.Right;
        }

        inserted.Parent = parent;

        if (ReferenceEquals(parent, _nil))
        {
            Root = inserted;
        }
        else if (_comparer.Compare(value, parent.Value) < 0)
        {
            parent.Left = inserted;
        }
        else
        {
            parent.Right = inserted;
        }

        Record(StepAction.AddNode, inserted, NodeColor.Red);

        FixAfterInsert(inserted);
        ResetNilLinks();

        return inserted;
    }

    private void FixAfterInsert(RBTreeNode<T> node)
    {
        while (node.Parent.Color == NodeColor.Red)
        {
            var parent = node.Parent;
            var grandparent = parent.Parent;
            var parentIsLeftChild = ReferenceEquals(parent, grandparent.Left);
            var uncle = parentIsLeftChild ? grandparent.Right : grandparent.Left;

            if (uncle.Color == NodeColor.Red)
            {
                Recolor(parent, NodeColor.Black);
                Recolor(uncle, NodeColor.Black);
                Recolor(grandparent, NodeColor.Red);
                node = grandparent;
                continue;
            }

            var nodeIsInnerChild = parentIsLeftChild
                ? ReferenceEquals(node, parent.Right)
                : ReferenceEquals(node, parent.Left);

            if (nodeIsInnerChild)
            {
                node = parent;

                if (parentIsLeftChild)
                {
                    RotateLeft(node);
                }
                else
                {
                    RotateRight(node);
                }

                parent = node.Parent;
                grandparent = parent.Parent;
            }

            Recolor(parent, NodeColor.Black);
            Recolor(grandparent, NodeColor.Red);

            if (parentIsLeftChild)
            {
                RotateRight(grandparent);
            }
            else
            {
                RotateLeft(grandparent);
            }
        }

        if (Root.Color != NodeColor.Black)
        {
            Recolor(Root, NodeColor.Black);
        }
    }

    public bool Delete(T value)
    {
        _recorder.Clear();
        ResetNilLinks();

        if (Count < AutoFocusThreshold)
        {
            FocusValue = string.Empty;
        }

        Record(StepAction.BeginDelete, ReferenceEquals(Root, _nil) ? null : Root.NodeId, Describe(value));

        var target = FindNode(value);

        if (ReferenceEquals(target, _nil))
        {
            return false;
        }

        RemoveNode(target);

        Count--;
        IsSearchHighlighted = false;

        Record(StepAction.DeleteComplete, nodeId: null, Describe(value));
        ResetNilLinks();

        return true;
    }

    private void RemoveNode(RBTreeNode<T> target)
    {
        var removed = target;
        var removedColor = removed.Color;
        RBTreeNode<T> replacement;

        if (ReferenceEquals(target.Left, _nil))
        {
            replacement = target.Right;
            Record(StepAction.DeleteCaseLeftNil, target);
            Transplant(target, target.Right);
        }
        else if (ReferenceEquals(target.Right, _nil))
        {
            replacement = target.Left;
            Record(StepAction.DeleteCaseRightNil, target);
            Transplant(target, target.Left);
        }
        else
        {
            Record(StepAction.FindMinimumStart, target);

            removed = MinimumNode(target.Right, record: true);
            removedColor = removed.Color;
            replacement = removed.Right;

            Record(StepAction.FindSuccessor, removed);

            if (ReferenceEquals(removed.Parent, target))
            {
                replacement.Parent = removed;
                Record(StepAction.SuccessorIsDirectChild, removed);
            }
            else
            {
                Record(StepAction.BeforeTransplantSuccessor, removed);

                Transplant(removed, removed.Right);
                removed.Right = target.Right;
                removed.Right.Parent = removed;

                Record(StepAction.AfterTransplantSuccessor, removed);
            }

            Record(StepAction.BeforeReplaceWithSuccessor, target);

            Transplant(target, removed);
            removed.Left = target.Left;
            removed.Left.Parent = removed;
            removed.Color = target.Color;

            Record(StepAction.AfterReplaceWithSuccessor, removed);
        }

        if (removedColor == NodeColor.Black)
        {
            Record(
                StepAction.BeginFixDelete,
                ReferenceEquals(replacement, _nil) ? NilNodeId : replacement.NodeId,
                ReferenceEquals(replacement, _nil) ? null : Describe(replacement.Value));

            FixAfterDelete(replacement);
        }
    }

    private void Transplant(RBTreeNode<T> u, RBTreeNode<T> v)
    {
        Record(StepAction.BeforeTransplant, u);

        if (ReferenceEquals(u.Parent, _nil))
        {
            Root = v;
        }
        else if (ReferenceEquals(u, u.Parent.Left))
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }

        v.Parent = u.Parent;

        Record(
            StepAction.AfterTransplant,
            ReferenceEquals(v, _nil) ? NilNodeId : v.NodeId,
            ReferenceEquals(v, _nil) ? null : Describe(v.Value));
    }

    private void FixAfterDelete(RBTreeNode<T> node)
    {
        while (!ReferenceEquals(node, Root) && node.Color == NodeColor.Black)
        {
            var isLeftChild = ReferenceEquals(node, node.Parent.Left);
            var sibling = isLeftChild ? node.Parent.Right : node.Parent.Left;

            if (sibling.Color == NodeColor.Red)
            {
                Record(isLeftChild ? StepAction.FixDeleteCase1 : StepAction.FixDeleteCase5, sibling);

                Recolor(sibling, NodeColor.Black);
                Recolor(node.Parent, NodeColor.Red);

                if (isLeftChild)
                {
                    RotateLeft(node.Parent);
                    sibling = node.Parent.Right;
                }
                else
                {
                    RotateRight(node.Parent);
                    sibling = node.Parent.Left;
                }
            }

            if (sibling.Left.Color == NodeColor.Black && sibling.Right.Color == NodeColor.Black)
            {
                Record(isLeftChild ? StepAction.FixDeleteCase2 : StepAction.FixDeleteCase6, sibling);

                Recolor(sibling, NodeColor.Red);
                node = node.Parent;
                continue;
            }

            var farChild = isLeftChild ? sibling.Right : sibling.Left;

            if (farChild.Color == NodeColor.Black)
            {
                Record(isLeftChild ? StepAction.FixDeleteCase3 : StepAction.FixDeleteCase7, sibling);

                var nearChild = isLeftChild ? sibling.Left : sibling.Right;
                Recolor(nearChild, NodeColor.Black);
                Recolor(sibling, NodeColor.Red);

                if (isLeftChild)
                {
                    RotateRight(sibling);
                    sibling = node.Parent.Right;
                }
                else
                {
                    RotateLeft(sibling);
                    sibling = node.Parent.Left;
                }
            }

            Record(isLeftChild ? StepAction.FixDeleteCase4 : StepAction.FixDeleteCase8, sibling);

            Recolor(sibling, node.Parent.Color);
            Recolor(node.Parent, NodeColor.Black);
            Recolor(isLeftChild ? sibling.Right : sibling.Left, NodeColor.Black);

            if (isLeftChild)
            {
                RotateLeft(node.Parent);
            }
            else
            {
                RotateRight(node.Parent);
            }

            node = Root;
        }

        if (node.Color != NodeColor.Black)
        {
            Recolor(node, NodeColor.Black);
        }
    }

    public RBTreeNode<T>? Search(T value)
    {
        _recorder.Clear();

        Record(StepAction.BeginSearch, nodeId: null, Describe(value));

        var found = FindNode(value);
        var success = !ReferenceEquals(found, _nil);

        IsSearchHighlighted = success;

        if (success)
        {
            FocusValue = Describe(found.Value) ?? string.Empty;
        }

        return success ? found : null;
    }

    private RBTreeNode<T> FindNode(T value)
    {
        var current = Root;

        while (!ReferenceEquals(current, _nil))
        {
            Record(StepAction.SearchStep, current);

            var comparison = _comparer.Compare(value, current.Value);

            if (comparison == 0)
            {
                Record(StepAction.SearchFound, current);
                return current;
            }

            Record(comparison < 0 ? StepAction.SearchGoLeft : StepAction.SearchGoRight, current);

            current = comparison < 0 ? current.Left : current.Right;
        }

        Record(StepAction.SearchNotFound, nodeId: null, Describe(value));

        return _nil;
    }

    private void RotateLeft(RBTreeNode<T> node)
    {
        Record(StepAction.BeforeRotateLeft, node);

        var pivot = node.Right;

        node.Right = pivot.Left;

        if (!ReferenceEquals(pivot.Left, _nil))
        {
            pivot.Left.Parent = node;
        }

        pivot.Parent = node.Parent;

        if (ReferenceEquals(node.Parent, _nil))
        {
            Root = pivot;
        }
        else if (ReferenceEquals(node, node.Parent.Left))
        {
            node.Parent.Left = pivot;
        }
        else
        {
            node.Parent.Right = pivot;
        }

        pivot.Left = node;
        node.Parent = pivot;

        Record(StepAction.AfterRotateLeft, node);
    }

    private void RotateRight(RBTreeNode<T> node)
    {
        Record(StepAction.BeforeRotateRight, node);

        var pivot = node.Left;

        node.Left = pivot.Right;

        if (!ReferenceEquals(pivot.Right, _nil))
        {
            pivot.Right.Parent = node;
        }

        pivot.Parent = node.Parent;

        if (ReferenceEquals(node.Parent, _nil))
        {
            Root = pivot;
        }
        else if (ReferenceEquals(node, node.Parent.Right))
        {
            node.Parent.Right = pivot;
        }
        else
        {
            node.Parent.Left = pivot;
        }

        pivot.Right = node;
        node.Parent = pivot;

        Record(StepAction.AfterRotateRight, node);
    }

    public RBTreeNode<T>? Minimum() => IsEmpty ? null : MinimumNode(Root, record: false);

    public RBTreeNode<T>? Maximum() => IsEmpty ? null : MaximumNode(Root, record: false);

    private RBTreeNode<T> MinimumNode(RBTreeNode<T> start, bool record)
    {
        var current = start;

        while (!ReferenceEquals(current.Left, _nil))
        {
            if (record)
            {
                Record(StepAction.FindMinimumStep, current);
            }

            current = current.Left;
        }

        return current;
    }

    private RBTreeNode<T> MaximumNode(RBTreeNode<T> start, bool record)
    {
        var current = start;

        while (!ReferenceEquals(current.Right, _nil))
        {
            if (record)
            {
                Record(StepAction.FindMaximumStep, current);
            }

            current = current.Right;
        }

        return current;
    }

    public int Height => HeightOf(Root);

    private int HeightOf(RBTreeNode<T> node) =>
        ReferenceEquals(node, _nil) ? 0 : 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));

    public int BlackHeight
    {
        get
        {
            var height = 0;
            var current = Root;

            while (!ReferenceEquals(current, _nil))
            {
                if (current.Color == NodeColor.Black)
                {
                    height++;
                }

                current = current.Left;
            }

            return height + 1;
        }
    }

    public IEnumerable<T> InOrder()
    {
        var stack = new Stack<RBTreeNode<T>>();
        var current = Root;

        while (!ReferenceEquals(current, _nil) || stack.Count > 0)
        {
            while (!ReferenceEquals(current, _nil))
            {
                stack.Push(current);
                current = current.Left;
            }

            current = stack.Pop();
            yield return current.Value;
            current = current.Right;
        }
    }

    public TreeNodeModel? ToSnapshot() => SnapshotOf(Root);

    private TreeNodeModel? SnapshotOf(RBTreeNode<T> node)
    {
        if (ReferenceEquals(node, _nil))
        {
            return null;
        }

        return new TreeNodeModel
        {
            NodeId = node.NodeId,
            Value = Describe(node.Value),
            Color = node.Color.ToString(),
            Left = SnapshotOf(node.Left),
            Right = SnapshotOf(node.Right)
        };
    }

    public void Restore(TreeNodeModel? snapshot, int nodeIdCounter, int count, string focusValue, bool isSearchHighlighted, Func<string, T> parseValue)
    {
        Root = RestoreNode(snapshot, _nil, parseValue);
        _nodeIdCounter = nodeIdCounter;
        Count = count;
        FocusValue = focusValue;
        IsSearchHighlighted = isSearchHighlighted;
        ResetNilLinks();
    }

    private RBTreeNode<T> RestoreNode(TreeNodeModel? snapshot, RBTreeNode<T> parent, Func<string, T> parseValue)
    {
        if (snapshot is null)
        {
            return _nil;
        }

        var color = Enum.TryParse<NodeColor>(snapshot.Color, ignoreCase: true, out var parsed)
            ? parsed
            : NodeColor.Black;

        var node = new RBTreeNode<T>(parseValue(snapshot.Value ?? string.Empty), snapshot.NodeId, color)
        {
            Parent = parent
        };

        node.Left = RestoreNode(snapshot.Left, node, parseValue);
        node.Right = RestoreNode(snapshot.Right, node, parseValue);

        return node;
    }

    public int NodeIdCounter => _nodeIdCounter;

    private string NextNodeId() => $"node_{_nodeIdCounter++}";

    private static string? Describe(T value) => value?.ToString();

    private void ResetNilLinks()
    {
        _nil.Parent = _nil;
        _nil.Left = _nil;
        _nil.Right = _nil;
        _nil.Color = NodeColor.Black;
    }

    private void Recolor(RBTreeNode<T> node, NodeColor color)
    {
        if (ReferenceEquals(node, _nil))
        {
            return;
        }

        node.Color = color;
        Record(StepAction.ColorChange, node, color);
    }

    private void Record(StepAction action, RBTreeNode<T> node, NodeColor? color = null)
    {
        if (!_recorder.IsEnabled)
        {
            return;
        }

        _recorder.Record(
            action,
            ToSnapshot,
            ReferenceEquals(node, _nil) ? NilNodeId : node.NodeId,
            ReferenceEquals(node, _nil) ? null : Describe(node.Value),
            color);
    }

    private void Record(StepAction action, string? nodeId, string? nodeValue = null)
    {
        if (!_recorder.IsEnabled)
        {
            return;
        }

        _recorder.Record(action, ToSnapshot, nodeId, nodeValue);
    }
}
