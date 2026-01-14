using Microsoft.AspNetCore.Components;

namespace LegacyLabeler.Components.Pages;

public partial class LegacyLabeler : ComponentBase
{
    [Parameter] public string? DocumentId { get; set; }
    [Inject] public NavigationManager Navigation { get; set; } = null!;
    
    private DocumentReview? currentDocument;
    private List<DocumentReview> allDocuments = new();
    private int currentDocumentIndex = 0;
    
    private string documentContent = "";
    private string documentName = "";
    private string documentDescription = "";
    private string selectedCategory = "";
    private string documentTags = "";
    private bool showSaveMessage = false;
    private List<DocumentDescription> savedDescriptions = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadDocuments();
        
        if (!string.IsNullOrEmpty(DocumentId))
        {
            await LoadSpecificDocument(DocumentId);
        }
        else
        {
            await LoadSampleDocument();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(DocumentId) && currentDocument?.Id != DocumentId)
        {
            await LoadSpecificDocument(DocumentId);
        }
    }

    private async Task LoadDocuments()
    {
        allDocuments = await DocumentService.ScanForDocumentsAsync();
        
        // Load existing review data and merge
        var reviewData = await DocumentService.LoadReviewDataAsync();
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

    private async Task LoadSpecificDocument(string documentId)
    {
        currentDocument = allDocuments.FirstOrDefault(d => d.Id == documentId);
        
        if (currentDocument != null)
        {
            currentDocumentIndex = allDocuments.IndexOf(currentDocument);
            documentName = currentDocument.OriginalFilename;
            documentDescription = currentDocument.EditedDescription;
            selectedCategory = currentDocument.Category;
            documentTags = currentDocument.Keywords;
            
            // Load document content for display
            if (File.Exists(currentDocument.FilePath))
            {
                if (currentDocument.FileType == "pdf")
                {
                    documentContent = $"PDF Document: {currentDocument.OriginalFilename}";
                }
                else
                {
                    documentContent = $"Image Document: {currentDocument.OriginalFilename}";
                }
            }
            
            currentDocument.Status = DocumentStatus.InReview;
            currentDocument.ReviewStarted = DateTime.UtcNow;
        }
    }

    private async Task LoadSampleDocument()
    {
        documentName = "Sample_Legacy_Document.txt";
        documentContent = @"MEMORANDUM

TO: All Department Heads
FROM: Executive Office
DATE: March 15, 1995
RE: Policy Update - Document Retention

This memorandum serves to notify all department heads of the updated document retention policy effective April 1, 1995.

Key Changes:
1. Financial records must be retained for 7 years (previously 5 years)
2. Personnel files must be maintained for the duration of employment plus 3 years
3. All correspondence must be filed within 30 days of receipt
4. Digital backup procedures are now mandatory for all critical documents

Please ensure your staff is informed of these changes and begin implementation immediately.

For questions regarding this policy, please contact the Records Management Office at extension 2847.

Sincerely,
Margaret Thompson
Executive Assistant";
    }

    private void ClearDocument()
    {
        documentContent = "";
        documentName = "";
        ClearDescription();
    }

    private void ClearDescription()
    {
        documentDescription = "";
        selectedCategory = "";
        documentTags = "";
        showSaveMessage = false;
    }

    private async Task SaveDescription()
    {
        if (currentDocument != null && !string.IsNullOrEmpty(documentDescription))
        {
            // Update the current document
            currentDocument.EditedDescription = documentDescription;
            currentDocument.Category = selectedCategory;
            currentDocument.Keywords = documentTags;
            currentDocument.Status = DocumentStatus.Completed;
            currentDocument.ReviewCompleted = DateTime.UtcNow;
            
            if (currentDocument.ReviewStarted.HasValue)
            {
                currentDocument.ReviewDurationSeconds = (int)(DateTime.UtcNow - currentDocument.ReviewStarted.Value).TotalSeconds;
            }

            // Save to JSON file
            await DocumentService.SaveDocumentReviewAsync(currentDocument);

            showSaveMessage = true;

            // Hide the save message after 3 seconds
            await Task.Delay(3000);
            showSaveMessage = false;
            StateHasChanged();
        }
    }

    private async Task SaveAndNext()
    {
        await SaveDescription();
        await LoadNextDocument();
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

    public class DocumentDescription
    {
        public string DocumentName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Tags { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}