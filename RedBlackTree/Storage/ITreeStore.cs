using RedBlackTree.Models;

namespace RedBlackTree.Storage;

public interface ITreeStore
{
    RedBlackTree<string> LoadTree();

    void SaveTree(RedBlackTree<string> tree);

    IReadOnlyList<TreeOperationStep> LoadSteps();

    void SaveSteps(IReadOnlyList<TreeOperationStep> steps);

    void Clear();
}
