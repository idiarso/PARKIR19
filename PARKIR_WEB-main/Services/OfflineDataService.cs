public interface IOfflineDataService
{
    Task SaveData(string data);
    Task<IEnumerable<string>> GetPendingData();
    Task MarkAsSynced(string id);
}

public class OfflineDataService : IOfflineDataService
{
    private readonly string _offlineDataPath;
    private readonly ILogger<OfflineDataService> _logger;

    public OfflineDataService(ILogger<OfflineDataService> logger)
    {
        _logger = logger;
        _offlineDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "offline_data");
        Directory.CreateDirectory(_offlineDataPath);
    }

    public async Task SaveData(string data)
    {
        var fileName = $"parking_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}.json";
        var path = Path.Combine(_offlineDataPath, fileName);
        await File.WriteAllTextAsync(path, data);
    }

    public async Task<IEnumerable<string>> GetPendingData()
    {
        var files = Directory.GetFiles(_offlineDataPath, "parking_*.json");
        var result = new List<string>();
        
        foreach (var file in files)
        {
            result.Add(await File.ReadAllTextAsync(file));
        }
        
        return result;
    }

    public Task MarkAsSynced(string id)
    {
        var path = Path.Combine(_offlineDataPath, $"parking_{id}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }
} 