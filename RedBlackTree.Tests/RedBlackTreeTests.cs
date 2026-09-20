using RedBlackTree.Comparers;
using RedBlackTree.Models;
using RedBlackTree.Serialization;
using Xunit;

namespace RedBlackTree.Tests;

public class RedBlackTreeTests
{
    private static RedBlackTree<string> NewTree() => new(NumericAwareStringComparer.Instance);

    private static void AssertValid(RedBlackTree<string> tree)
    {
        var nil = tree.Nil;
        var nodeCount = 0;
        var blackHeights = new HashSet<int>();

        Assert.Equal(NodeColor.Black, tree.Root.Color);

        void Walk(RBTreeNode<string> node, int blackCount)
        {
            if (ReferenceEquals(node, nil))
            {
                blackHeights.Add(blackCount + 1);
                return;
            }

            nodeCount++;

            if (node.Color == NodeColor.Black)
            {
                blackCount++;
            }
            else
            {
                Assert.Equal(NodeColor.Black, node.Left.Color);
                Assert.Equal(NodeColor.Black, node.Right.Color);
            }

            Walk(node.Left, blackCount);
            Walk(node.Right, blackCount);
        }

        Walk(tree.Root, 0);

        Assert.Single(blackHeights);
        Assert.Equal(tree.Count, nodeCount);

        var values = tree.InOrder().ToList();

        for (var i = 1; i < values.Count; i++)
        {
            Assert.True(NumericAwareStringComparer.Instance.Compare(values[i - 1], values[i]) <= 0);
        }

        if (tree.Count > 0)
        {
            Assert.True(tree.Height <= 2 * Math.Log2(tree.Count + 1));
        }
    }

    [Fact]
    public void Insert_KeepsTreeBalanced()
    {
        var tree = NewTree();

        for (var i = 1; i <= 500; i++)
        {
            tree.Insert(i.ToString());
            AssertValid(tree);
        }

        Assert.Equal(500, tree.Count);
    }

    [Fact]
    public void InsertAndDelete_KeepTreeValid()
    {
        var random = new Random(20260918);
        var tree = NewTree();
        var expected = new List<string>();

        for (var step = 0; step < 600; step++)
        {
            if (expected.Count == 0 || random.NextDouble() < 0.6)
            {
                var value = random.Next(0, 150).ToString();
                tree.Insert(value);
                expected.Add(value);
            }
            else
            {
                var index = random.Next(expected.Count);
                Assert.True(tree.Delete(expected[index]));
                expected.RemoveAt(index);
            }

            AssertValid(tree);
        }

        expected.Sort(NumericAwareStringComparer.Instance);
        Assert.Equal(expected, tree.InOrder());
    }

    [Fact]
    public void SeparateTrees_DoNotAffectEachOther()
    {
        var trees = Enumerable.Range(0, 8).Select(_ => NewTree()).ToArray();

        Assert.NotSame(trees[0].Nil, trees[1].Nil);

        foreach (var tree in trees)
        {
            for (var value = 0; value < 40; value++)
            {
                tree.Insert(value.ToString());
            }
        }

        Parallel.For(0, trees.Length, index =>
        {
            var random = new Random(index);

            for (var step = 0; step < 100; step++)
            {
                trees[index].Delete(random.Next(0, 40).ToString());
                trees[index].Insert(random.Next(0, 40).ToString());
            }
        });

        foreach (var tree in trees)
        {
            AssertValid(tree);
        }
    }

    [Fact]
    public void Search_AndNumericOrdering_WorkCorrectly()
    {
        var tree = NewTree();

        foreach (var value in new[] { "10", "9", "100", "20", "3", "abc" })
        {
            tree.Insert(value);
        }

        Assert.Equal(new[] { "3", "9", "10", "20", "100", "abc" }, tree.InOrder());

        Assert.NotNull(tree.Search("100"));
        Assert.NotNull(tree.Search("abc"));
        Assert.Null(tree.Search("missing"));

        Assert.False(tree.Delete("missing"));
        Assert.Equal(6, tree.Count);
    }

    [Fact]
    public void SaveAndLoad_PreserveTheTree()
    {
        ITreeSerializer serializer = new JsonTreeSerializer();

        var original = NewTree();

        for (var i = 0; i < 60; i++)
        {
            original.Insert(i.ToString());
        }

        original.Delete("30");

        var restored = serializer.Deserialize(serializer.Serialize(original));

        Assert.Equal(original.Count, restored.Count);
        Assert.Equal(original.InOrder(), restored.InOrder());

        restored.Insert("999");
        AssertValid(restored);

        const string legacyFile = """
        {
          "Tree": {
            "Value": "50", "NodeId": "node_0", "Color": "Black",
            "Left":  { "Value": "30", "NodeId": "node_1", "Color": "Red", "Left": null, "Right": null },
            "Right": { "Value": "70", "NodeId": "node_2", "Color": "Red", "Left": null, "Right": null }
          },
          "NewNodeValue": "", "_nodeIdCounter": 3, "InsertSteps": [],
          "Quantity": 3, "isSearchClicked": false
        }
        """;

        var legacy = serializer.Deserialize(legacyFile);

        Assert.Equal(3, legacy.Count);
        Assert.Equal(new[] { "30", "50", "70" }, legacy.InOrder());
    }
}
