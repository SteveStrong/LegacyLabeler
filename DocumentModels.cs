namespace LegacyLabeler;

public class DocumentReviewData
{
    public ReviewMetadata Metadata { get; set; } = new();
    public List<DocumentReview> Documents { get; set; } = new();
}

public class DocumentReview
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OriginalFilename { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileType { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string AssignedReviewer { get; set; } = "";
    
    // Review Data
    public string VoiceTranscription { get; set; } = "";
    public string EditedDescription { get; set; } = "";
    public string Category { get; set; } = "";
    public string Keywords { get; set; } = "";
    public string NewFilename { get; set; } = "";
    
    // Tracking
    public DateTime? ReviewStarted { get; set; }
    public DateTime? ReviewCompleted { get; set; }
    public int ReviewDurationSeconds { get; set; }
    public string ReviewerId { get; set; } = "";
}

public class ReviewMetadata
{
    public string Version { get; set; } = "1.0";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public int TotalDocuments { get; set; }
    public int CompletedReviews { get; set; }
    public List<string> Categories { get; set; } = new() 
    { 
        "Engineering", 
        "Mechanical", 
        "Electrical", 
        "Process", 
        "Administrative" 
    };
    public List<string> Reviewers { get; set; } = new();
}

public enum DocumentStatus
{
    Pending,
    InReview,
    Completed,
    Skipped,
    NeedsAttention
}