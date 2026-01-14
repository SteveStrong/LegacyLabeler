using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace LegacyLabeler.Components.Pages;

public partial class LegacyLabeler : ComponentBase
{
    [Inject] private DocumentService DocumentService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] [SupplyParameterFromQuery] public string? DocumentId { get; set; }

    private DocumentReview? currentDocument;
    private DocumentReviewData? reviewData;
    private List<DocumentReview> allDocuments = new();
    private int currentDocumentIndex = 0;
    private bool isLoading = true;
    private string description = "";
    private string selectedCategory = "";
    private string keywords = "";
    private bool showSaveMessage = false;
    private bool isRecording = false;
    private bool imageZoomed = false;
    
    // Legacy support variables
    private string documentDescription => description;
    private string documentTags => keywords;
    
    // Activity log for voice capture events
    private List<ActivityLogEntry> activityLog = new();
    private List<SavedDescription> savedDescriptions = new();
    
    private readonly List<string> categories = new()
    {
        "Engineering Drawing",
        "Schematic",
        "Blueprint", 
        "Technical Manual",
        "Installation Guide",
        "Maintenance Log",
        "Quality Control",
        "Safety Procedure",
        "Other"
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadDocuments();
        await LoadDocumentData();
        await LoadSavedDescriptions();
    }

    private async Task LoadDocuments()
    {
        allDocuments = await DocumentService.ScanForDocumentsAsync();
        
        // Load existing review data and merge
        reviewData = await DocumentService.LoadReviewDataAsync();
        foreach (var doc in allDocuments)
        {
            var existing = reviewData.Documents.FirstOrDefault(d => d.Id == doc.Id);
            if (existing != null)
            {
                doc.Status = existing.Status;
                doc.EditedDescription = existing.EditedDescription;
                doc.Category = existing.Category;
                doc.Keywords = existing.Keywords;
                doc.VoiceTranscription = existing.VoiceTranscription;
            }
        }
    }

    private async Task LoadDocumentData()
    {
        if (string.IsNullOrEmpty(DocumentId))
        {
            // Load the first document if no ID provided
            if (allDocuments.Any())
            {
                currentDocument = allDocuments.First();
                currentDocumentIndex = 0;
            }
            else
            {
                AddToActivityLog("No documents found");
                return;
            }
        }
        else
        {
            currentDocument = allDocuments.FirstOrDefault(d => d.Id == DocumentId);
            if (currentDocument != null)
            {
                currentDocumentIndex = allDocuments.IndexOf(currentDocument);
            }
        }
        
        if (currentDocument != null)
        {
            description = currentDocument.EditedDescription ?? "";
            selectedCategory = currentDocument.Category ?? "";
            keywords = currentDocument.Keywords ?? "";
            AddToActivityLog($"Loaded document: {currentDocument.OriginalFilename}");
        }
        else
        {
            AddToActivityLog($"Document not found: {DocumentId}");
        }
        
        isLoading = false;
    }
    
    private async Task LoadSavedDescriptions()
    {
        // Load recent saved descriptions for reference
        savedDescriptions = reviewData?.Documents
            .Where(d => !string.IsNullOrEmpty(d.EditedDescription))
            .OrderByDescending(d => d.ReviewCompleted ?? d.ImportDate)
            .Take(5)
            .Select(d => new SavedDescription
            {
                DocumentName = d.OriginalFilename,
                Description = d.EditedDescription ?? "",
                Category = d.Category,
                Timestamp = d.ReviewCompleted ?? d.ImportDate
            })
            .ToList() ?? new List<SavedDescription>();
    }

    private async Task OnSpeechCaptured(string speechText)
    {
        try
        {
            AddToActivityLog($"Speech captured: {speechText.Substring(0, Math.Min(50, speechText.Length))}...");
            
            // Accumulate speech text to existing description
            if (!string.IsNullOrEmpty(description))
            {
                description += " " + speechText;
            }
            else
            {
                description = speechText;
            }
            
            await InvokeAsync(StateHasChanged);
            
            ShowNotification("Speech captured successfully", NotificationSeverity.Success);
        }
        catch (Exception ex)
        {
            AddToActivityLog($"Error capturing speech: {ex.Message}");
            ShowNotification("Error capturing speech", NotificationSeverity.Error);
        }
    }

    private async Task OnDescriptionChange(string value)
    {
        description = value;
        AddToActivityLog("Description updated manually");
        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveDescription()
    {
        if (currentDocument == null) return;

        try
        {
            currentDocument.EditedDescription = description;
            currentDocument.Category = selectedCategory;
            currentDocument.Keywords = keywords;
            currentDocument.ReviewCompleted = DateTime.UtcNow;
            currentDocument.Status = DocumentStatus.Completed;

            await DocumentService.SaveDocumentReviewAsync(currentDocument);
            await LoadSavedDescriptions();
            
            AddToActivityLog($"Saved description for {currentDocument.OriginalFilename}");
            ShowNotification("Description saved successfully!", NotificationSeverity.Success);
            
            showSaveMessage = true;
            StateHasChanged();
            
            // Auto-hide success message
            await Task.Delay(3000);
            showSaveMessage = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            AddToActivityLog($"Error saving: {ex.Message}");
            ShowNotification("Error saving description", NotificationSeverity.Error);
        }
    }

    private void BackToDocuments()
    {
        Navigation.NavigateTo("/documentbrowser");
    }
    
    private void AddToActivityLog(string message)
    {
        activityLog.Add(new ActivityLogEntry
        {
            Timestamp = DateTime.Now,
            Message = message
        });
        
        // Keep only last 20 entries
        if (activityLog.Count > 20)
        {
            activityLog.RemoveAt(0);
        }
    }
    
    private void ShowNotification(string message, NotificationSeverity severity)
    {
        NotificationService.Notify(new NotificationMessage
        {
            Severity = severity,
            Summary = message,
            Duration = 3000
        });
    }

    private string GetDocumentUrl()
    {
        if (currentDocument == null) return "";
        return $"/Documents/{currentDocument.OriginalFilename}";
    }
    
    private static bool IsImageFile(string fileType)
    {
        return fileType.ToLower() switch
        {
            "jpg" or "jpeg" or "png" or "gif" or "bmp" or "tiff" or "tif" => true,
            _ => false
        };
    }

    private void ToggleImageZoom()
    {
        imageZoomed = !imageZoomed;
        StateHasChanged();
    }

    private async Task LoadNextDocument()
    {
        if (allDocuments.Count > 0)
        {
            currentDocumentIndex = (currentDocumentIndex + 1) % allDocuments.Count;
            var nextDoc = allDocuments[currentDocumentIndex];
            Navigation.NavigateTo($"/legacy-labeler/{nextDoc.Id}");
        }
    }

    private async Task LoadPreviousDocument()
    {
        if (allDocuments.Count > 0)
        {
            currentDocumentIndex = currentDocumentIndex > 0 ? currentDocumentIndex - 1 : allDocuments.Count - 1;
            var prevDoc = allDocuments[currentDocumentIndex];
            Navigation.NavigateTo($"/legacy-labeler/{prevDoc.Id}");
        }
    }

    private async Task SaveAndNext()
    {
        await SaveDescription();
        await LoadNextDocument();
    }

    private void ClearDescription()
    {
        description = "";
        selectedCategory = "";
        keywords = "";
        showSaveMessage = false;
    }

    private class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = "";
    }
    
    private class SavedDescription
    {
        public string DocumentName { get; set; } = "";
        public string Description { get; set; } = "";
        public string? Category { get; set; }
        public DateTime Timestamp { get; set; }
    }
}