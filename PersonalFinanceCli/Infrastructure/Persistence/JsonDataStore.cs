using System.Text.Json;
using PersonalFinanceCli.Domain.Entities;

namespace PersonalFinanceCli.Infrastructure.Persistence;

/// Handles the low-level file operations and JSON serialization for the application's data store.
public sealed class JsonDataStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;

    public JsonDataStore(string filePath)
    {
        _filePath = filePath;
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public DataFile Load()
    {
        if (!File.Exists(_filePath))
        {
            var empty = new DataFile();
            Save(empty);
            return empty;
        }

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            var empty = new DataFile();
            Save(empty);
            return empty;
        }

        var result = JsonSerializer.Deserialize<DataFile>(json, _options);
        if (result == null)
        {
            var empty = new DataFile();
            Save(empty);
            return empty;
        }

        // Ensure collections are initialized to prevent NullReferenceExceptions 
        // if the JSON payload is missing these properties.
        result.Cards ??= new List<Card>();
        result.Transactions ??= new List<Transaction>();
        result.DailyLimits ??= new List<DailyLimit>();

        return result;
    }

    public void Save(DataFile data)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText(_filePath, json);
    }
}

/// Represents the root structure of the JSON database payload.
public sealed class DataFile
{
    public List<Card> Cards { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new();
    public List<DailyLimit> DailyLimits { get; set; } = new();
    public DateOnly? LastCushionDeclinedDate { get; set; }
    public bool HasSeenOnboarding { get; set; }
    public Guid? DefaultCardId { get; set; }
}