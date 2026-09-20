using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RedBlackTree.Models;
using RedBlackTree.Services;

namespace RedBlackTree.Controllers;

public sealed class HomeController : Controller
{
    private const string MessageKey = "Message";

    private readonly IRedBlackTreeService _treeService;

    public HomeController(IRedBlackTreeService treeService)
    {
        _treeService = treeService;
    }

    [HttpGet]
    public IActionResult Index() =>
        View(_treeService.GetView(TempData[MessageKey] as string));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(TreeCommand action, string? value)
    {
        var result = action switch
        {
            TreeCommand.Insert => _treeService.Insert(value),
            TreeCommand.Delete => _treeService.Delete(value),
            TreeCommand.Search => _treeService.Search(value),
            _ => OperationResult.Ignored
        };

        return RedirectWithMessage(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ManageTree(ManageCommand action)
    {
        var result = action switch
        {
            ManageCommand.DeleteAll => _treeService.Clear(),
            ManageCommand.Info => _treeService.GetStatistics(),
            _ => OperationResult.Ignored
        };

        return RedirectWithMessage(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult HandleFiles(FileCommand func, IFormFile? file)
    {
        if (func == FileCommand.Export)
        {
            var export = _treeService.Export();

            if (export.Success)
            {
                return File(export.Content!, "application/json", export.FileName!);
            }

            TempData[MessageKey] = export.Error;
            return RedirectToAction(nameof(Index));
        }

        return RedirectWithMessage(_treeService.Import(file));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private IActionResult RedirectWithMessage(OperationResult result)
    {
        if (!string.IsNullOrEmpty(result.Message))
        {
            TempData[MessageKey] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
