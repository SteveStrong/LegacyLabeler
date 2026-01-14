using System.Text.Json;
using LegacyLabeler.Data;

namespace LegacyLabeler.Data;

public class DocumentService
{
    private readonly string _documentsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Documents");
    private readonly string _reviewDataFile = Path.Combine(Directory.GetCurrentDirectory(), "ReviewData", "review_data.json");
    private DocumentReviewData? _cache;

    public async Task<List<DocumentReview>> ScanForDocumentsAsync()
    {
        var supportedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif" };
        
        if (!Directory.Exists(_documentsFolder))
        {
            Directory.CreateDirectory(_documentsFolder);
            return new List<DocumentReview>();
        }

        var files = Directory.GetFiles(_documentsFolder, "*.*", SearchOption.AllDirectories)
            .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
            .ToList();

        var documents = new List<DocumentReview>();
        
        foreach (var filePath in files)
        {
            var fileInfo = new FileInfo(filePath);
            var doc = new DocumentReview
            {
                Id = GenerateDocumentId(filePath),
                OriginalFilename = fileInfo.Name,
                FilePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath),
                FileType = fileInfo.Extension.ToLower().TrimStart('.'),
                FileSize = fileInfo.Length,
                ImportDate = DateTime.UtcNow
            };
            documents.Add(doc);
        }

        return documents;
    }

    public async Task<DocumentReviewData> LoadReviewDataAsync()
    {
        if (_cache != null) return _cache;

        if (!File.Exists(_reviewDataFile))
        {
            _cache = new DocumentReviewData();
            await SaveReviewDataAsync(_cache);
            return _cache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_reviewDataFile);
            _cache = JsonSerializer.Deserialize<DocumentReviewData>(json) ?? new DocumentReviewData();
            return _cache;
        }
        catch
        {
            _cache = new DocumentReviewData();
            return _cache;
        }
    }

    public async Task SaveReviewDataAsync(DocumentReviewData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_reviewDataFile)!);
        
        // Update metadata
        data.Metadata.LastUpdated = DateTime.UtcNow;
        data.Metadata.TotalDocuments = data.Documents.Count;
        data.Metadata.CompletedReviews = data.Documents.Count(d => d.Status == DocumentStatus.Completed);

        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(data, options);
        await File.WriteAllTextAsync(_reviewDataFile, json);
        _cache = data;
    }

    public async Task SaveDocumentReviewAsync(DocumentReview review)
    {
        var data = await LoadReviewDataAsync();
        
        var existing = data.Documents.FirstOrDefault(d => d.Id == review.Id);
        if (existing != null)
        {
            var index = data.Documents.IndexOf(existing);
            data.Documents[index] = review;
        }
        else
        {
            data.Documents.Add(review);
        }

        await SaveReviewDataAsync(data);
    }

    private static string GenerateDocumentId(string filePath)
    {
        // Simple ID generation based on file path hash
        return $"doc_{Math.Abs(filePath.GetHashCode()):X8}";
    }
}