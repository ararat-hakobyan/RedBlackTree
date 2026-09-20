using RedBlackTree.Models;

namespace RedBlackTree.Serialization;

public interface ITreeSerializer
{
    string Serialize(RedBlackTree<string> tree);

    byte[] SerializeToBytes(RedBlackTree<string> tree);

    RedBlackTree<string> Deserialize(string json);

    RedBlackTree<string> Deserialize(ReadOnlySpan<byte> utf8Json);
}
