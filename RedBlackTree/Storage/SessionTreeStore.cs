using System.Text.Json;
using System.Text.Json.Serialization;
using RedBlackTree.Models;
using RedBlackTree.Serialization;

namespace RedBlackTree.Storage;

public sealed class SessionTreeStore : ITreeStore
{
    private const string UserIdKey = "rbtree:user";
    private const string TreeKeyPrefix = "rbtree:tree:";
    private const string StepsKeyPrefix = "rbtree:steps:";

    private static readonly JsonSerializerOptions StepOptions = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITreeSerializer _serializer;
    private readonly ILogger<SessionTreeStore> _logger;

    public SessionTreeStore(
        IHttpContextAccessor httpContextAccessor,
        ITreeSerializer serializer,
        ILogger<SessionTreeStore> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _serializer = serializer;
        _logger = logger;
    }

    private ISession Session =>
        _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("The session is not available. Make sure app.UseSession() is called.");

    private string UserId
    {
        get
        {
            var userId = Session.GetString(UserIdKey);

            if (string.IsNullOrEmpty(userId))
            {
                userId = Guid.NewGuid().ToString("N");
                Session.SetString(UserIdKey, userId);
                _logger.LogInformation("Created a new visitor id {UserId}", userId);
            }

            return userId;
        }
    }

    public RedBlackTree<string> LoadTree()
    {
        var json = Session.GetString(TreeKeyPrefix + UserId);

        if (string.IsNullOrEmpty(json))
        {
            return _serializer.Deserialize("null");
        }

        try
        {
            return _serializer.Deserialize(json);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "The tree stored in the session is corrupt, starting a new one");
            return _serializer.Deserialize("null");
        }
    }

    public void SaveTree(RedBlackTree<string> tree) =>
        Session.SetString(TreeKeyPrefix + UserId, _serializer.Serialize(tree));

    public IReadOnlyList<TreeOperationStep> LoadSteps()
    {
        var json = Session.GetString(StepsKeyPrefix + UserId);

        if (string.IsNullOrEmpty(json))
        {
            return Array.Empty<TreeOperationStep>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<TreeOperationStep>>(json, StepOptions)
                   ?? (IReadOnlyList<TreeOperationStep>)Array.Empty<TreeOperationStep>();
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "The animation steps could not be read");
            return Array.Empty<TreeOperationStep>();
        }
    }

    public void SaveSteps(IReadOnlyList<TreeOperationStep> steps)
    {
        var key = StepsKeyPrefix + UserId;

        if (steps.Count == 0)
        {
            Session.Remove(key);
            return;
        }

        Session.SetString(key, JsonSerializer.Serialize(steps, StepOptions));
    }

    public void Clear()
    {
        Session.Remove(TreeKeyPrefix + UserId);
        Session.Remove(StepsKeyPrefix + UserId);
    }
}
