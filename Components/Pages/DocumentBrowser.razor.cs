using Microsoft.AspNetCore.Components;

namespace LegacyLabeler.Components.Pages;

public partial class DocumentBrowser : ComponentBase
{
    private List<DocumentReview> documents = new();
    private bool isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        await ScanDocuments();
    }

    private async Task ScanDocuments()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            documents = await DocumentService.ScanForDocumentsAsync();
            
            // Load existing review data and merge status
            var reviewData = await DocumentService.LoadReviewDataAsync();
            
            foreach (var doc in documents)
            {
                var existing = reviewData.Documents.FirstOrDefault(d => d.Id == doc.Id);
                if (existing != null)
                {
                    doc.Status = existing.Status;
                    doc.EditedDescription = existing.EditedDescription;
                    doc.Category = existing.Category;
                }
            }
        }
        catch (Exception ex)
        {
            // For demo purposes, just log to console
            Console.WriteLine($"Error scanning documents: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void StartReview(string documentId)
    {
        Navigation.NavigateTo($"/legacy-labeler/{documentId}");
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }

    private static string GetStatusBadgeClass(DocumentStatus status)
    {
        return status switch
        {
            DocumentStatus.Completed => "bg-success",
            DocumentStatus.InReview => "bg-warning",
            DocumentStatus.Pending => "bg-secondary",
            DocumentStatus.Skipped => "bg-dark",
            DocumentStatus.NeedsAttention => "bg-danger",
            _ => "bg-secondary"
        };
    }
}