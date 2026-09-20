using System.Text;
using System.Text.Json;
using RedBlackTree.Comparers;
using RedBlackTree.Models;

namespace RedBlackTree.Serialization;

public sealed class JsonTreeSerializer : ITreeSerializer
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = false
    };

    public string Serialize(RedBlackTree<string> tree) =>
        JsonSerializer.Serialize(ToDto(tree), WriteOptions);

    public byte[] SerializeToBytes(RedBlackTree<string> tree) =>
        JsonSerializer.SerializeToUtf8Bytes(ToDto(tree), new JsonSerializerOptions { WriteIndented = true });

    public RedBlackTree<string> Deserialize(string json) =>
        FromDto(JsonSerializer.Deserialize<TreeSnapshotDto>(json));

    public RedBlackTree<string> Deserialize(ReadOnlySpan<byte> utf8Json) =>
        FromDto(JsonSerializer.Deserialize<TreeSnapshotDto>(utf8Json));

    private static TreeSnapshotDto ToDto(RedBlackTree<string> tree) => new()
    {
        Tree = tree.ToSnapshot(),
        FocusValue = tree.FocusValue,
        NodeIdCounter = tree.NodeIdCounter,
        Count = tree.Count,
        IsSearchHighlighted = tree.IsSearchHighlighted
    };

    private static RedBlackTree<string> FromDto(TreeSnapshotDto? dto)
    {
        var tree = new RedBlackTree<string>(NumericAwareStringComparer.Instance);

        if (dto is null)
        {
            return tree;
        }

        tree.Restore(
            dto.Tree,
            dto.NodeIdCounter,
            dto.Count,
            dto.FocusValue ?? string.Empty,
            dto.IsSearchHighlighted,
            value => value);

        return tree;
    }

    internal static string Describe(byte[] utf8Json) => Encoding.UTF8.GetString(utf8Json);
}
