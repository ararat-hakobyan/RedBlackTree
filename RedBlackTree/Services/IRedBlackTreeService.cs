using RedBlackTree.Models;

namespace RedBlackTree.Services;

public interface IRedBlackTreeService
{
    TreeViewModel GetView(string? message = null);

    OperationResult Insert(string? value);

    OperationResult Delete(string? value);

    OperationResult Search(string? value);

    OperationResult Clear();

    OperationResult GetStatistics();

    ExportResult Export();

    OperationResult Import(IFormFile? file);
}
