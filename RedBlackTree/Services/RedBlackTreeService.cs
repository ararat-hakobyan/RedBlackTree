using System.Text.Json;
using RedBlackTree.Models;
using RedBlackTree.Recording;
using RedBlackTree.Resources;
using RedBlackTree.Serialization;
using RedBlackTree.Storage;

namespace RedBlackTree.Services;

public sealed class RedBlackTreeService : IRedBlackTreeService
{
    private const long MaxImportBytes = 2 * 1024 * 1024;

    private readonly ITreeStore _store;
    private readonly ITreeSerializer _serializer;
    private readonly ILogger<RedBlackTreeService> _logger;

    public RedBlackTreeService(
        ITreeStore store,
        ITreeSerializer serializer,
        ILogger<RedBlackTreeService> logger)
    {
        _store = store;
        _serializer = serializer;
        _logger = logger;
    }

    public TreeViewModel GetView(string? message = null)
    {
        var tree = _store.LoadTree();

        return new TreeViewModel
        {
            Message = message,
            Payload = new TreePayload
            {
                Root = tree.ToSnapshot(),
                Steps = _store.LoadSteps(),
                FocusValue = tree.FocusValue,
                Count = tree.Count,
                IsSearchHighlighted = tree.IsSearchHighlighted,
                AutoFocusThreshold = RedBlackTree<string>.AutoFocusThreshold
            }
        };
    }

    public OperationResult Insert(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return OperationResult.Ignored;
        }

        var tree = LoadTreeWithRecorder(out var recorder);
        tree.Insert(value);
        Persist(tree, recorder);

        return OperationResult.Ok();
    }

    public OperationResult Delete(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return OperationResult.Ignored;
        }

        var tree = LoadTreeWithRecorder(out var recorder);
        var removed = tree.Delete(value);
        Persist(tree, recorder);

        return removed
            ? OperationResult.Ok(Messages.Removed(value))
            : OperationResult.Fail(Messages.NotFound(value));
    }

    public OperationResult Search(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return OperationResult.Ignored;
        }

        var tree = LoadTreeWithRecorder(out var recorder);
        var found = tree.Search(value) is not null;
        Persist(tree, recorder);

        return found
            ? OperationResult.Ok(Messages.Found(value))
            : OperationResult.Fail(Messages.NotFound(value));
    }

    public OperationResult Clear()
    {
        _store.Clear();
        return OperationResult.Ok();
    }

    public OperationResult GetStatistics()
    {
        var tree = _store.LoadTree();

        if (tree.Count <= 1)
        {
            return OperationResult.Ignored;
        }

        var minimum = tree.Minimum();
        var maximum = tree.Maximum();

        if (minimum is null || maximum is null)
        {
            return OperationResult.Ignored;
        }

        _store.SaveSteps(Array.Empty<TreeOperationStep>());

        return OperationResult.Ok(
            Messages.Statistics(tree.Count, minimum.Value, maximum.Value, tree.Height));
    }

    public ExportResult Export()
    {
        var tree = _store.LoadTree();

        if (tree.IsEmpty)
        {
            return ExportResult.Fail(Messages.EmptyTree);
        }

        var fileName = $"RedBlackTree_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json";

        return ExportResult.Ok(_serializer.SerializeToBytes(tree), fileName);
    }

    public OperationResult Import(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return OperationResult.Fail(Messages.FileNotSelected);
        }

        if (file.Length > MaxImportBytes)
        {
            return OperationResult.Fail(Messages.TooLarge(MaxImportBytes));
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Fail(Messages.OnlyJsonAllowed);
        }

        try
        {
            using var buffer = new MemoryStream();
            file.CopyTo(buffer);

            if (buffer.Length == 0)
            {
                return OperationResult.Fail(Messages.FileIsEmpty);
            }

            var tree = _serializer.Deserialize(buffer.ToArray());

            _store.SaveTree(tree);
            _store.SaveSteps(Array.Empty<TreeOperationStep>());

            return OperationResult.Ok();
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "The imported file is not valid JSON");
            return OperationResult.Fail(Messages.InvalidJson);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Importing the file failed");
            return OperationResult.Fail(Messages.ImportFailed);
        }
    }

    private RedBlackTree<string> LoadTreeWithRecorder(out TreeStepRecorder<string> recorder)
    {
        var tree = _store.LoadTree();
        recorder = new TreeStepRecorder<string>();
        tree.UseRecorder(recorder);
        return tree;
    }

    private void Persist(RedBlackTree<string> tree, TreeStepRecorder<string> recorder)
    {
        _store.SaveTree(tree);
        _store.SaveSteps(recorder.Steps);
    }
}
