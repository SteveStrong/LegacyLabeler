# Legacy Labeler Application - Design & Execution Plan

**Project**: Voice-Assisted Document Triage Application  
**Date**: January 14, 2026  
**Technology Stack**: Blazor Server, Radzen Components, Entity Framework Core, SQLite  
**Objective**: Catalog 2,567 technical drawings through efficient SME voice-assisted review  

---

## 📋 Project Overview

### The Problem
- **2,567 technical drawings** (993 PDFs + 1,574 scanned mechanicals) are unusable
- Documents lack meaningful names, metadata, and organization
- Traditional manual typing approaches take 5-10 minutes per document (impractical)
- SME knowledge required for proper identification and categorization

### The Solution
Voice-assisted triage application that reduces review time to **~1 minute per document** using:
- High-quality document viewer
- Voice-to-text transcription (local processing)
- Streamlined metadata capture workflow
- Multi-user progress tracking

### Success Criteria
- ✅ 2,567 documents properly named and cataloged
- ✅ Searchable text descriptions for document discovery
- ✅ Ready for SharePoint migration
- ✅ Foundation for future AI/RAG initiatives

---

## 🏗️ Technical Architecture

### Core Technology Stack
- **Frontend**: Blazor Server (.NET 8/10)
- **UI Components**: Radzen Blazor Components (voice capture, forms, data grids)
- **Backend**: ASP.NET Core
- **Data Storage**: JSON files (Phase 1 - simple approach)
- **File Processing**: PDF.js/PDFium for document rendering
- **Voice Processing**: Radzen voice capture components
- **Deployment**: Self-contained desktop-style deployment

### Data Storage Architecture (Phase 1)

**Selected Approach: Single JSON File**

All document review data will be stored in a single `review_data.json` file in the `ReviewData` folder. This provides the optimal balance of simplicity, performance, and maintainability for Phase 1.

**JSON Structure:**
```json
{
  "metadata": {
    "version": "1.0",
    "lastUpdated": "2026-01-14T15:30:00Z",
    "totalDocuments": 2567,
    "completedReviews": 247,
    "categories": ["Engineering", "Mechanical", "Electrical", "Process", "Administrative"],
    "reviewers": ["mike.thompson", "sarah.chen", "bob.johnson"]
  },
  "documents": [
    {
      "id": "doc_001",
      "originalFilename": "Drawing_347.pdf",
      "filePath": "./Documents/Drawing_347.pdf",
      "fileType": "pdf",
      "fileSize": 2048576,
      "importDate": "2026-01-14T10:00:00Z",
      "status": "completed",
      "assignedReviewer": "mike.thompson",
      "reviewData": {
        "voiceTranscription": "This is the Unit 2 cooling water pump assembly revision C shows motor mount and piping connections dated 2023",
        "editedDescription": "Unit 2 cooling water pump assembly, revision C, motor mount and piping connections, dated 2023",
        "category": "Mechanical",
        "keywords": "pump, assembly, unit 2, cooling water, motor mount, revision C",
        "newFilename": "Unit2_CoolingWater_Pump_Assembly_RevC_2023.pdf",
        "reviewStarted": "2026-01-14T10:30:00Z",
        "reviewCompleted": "2026-01-14T10:31:15Z",
        "reviewDurationSeconds": 75,
        "reviewerId": "mike.thompson"
      }
    }
  ]
}
```

**Alternative Options (Future Consideration):**
- Individual JSON files per document (better for very large datasets)
- Direct CSV output (immediate Excel compatibility)
- Database migration (when scaling beyond 1000+ documents)

### Application Structure
```
LegacyLabeler/
├── Components/
│   ├── Pages/
│   │   ├── DocumentReview.razor      # Main review interface
│   │   ├── Dashboard.razor           # Progress tracking
│   │   ├── DocumentBrowser.razor     # Document selection
│   │   └── Reports.razor             # Export and reporting
│   ├── Layout/
│   └── Shared/
│       ├── VoiceCapture.razor        # Radzen voice component wrapper
│       ├── DocumentViewer.razor      # PDF/image display
│       └── ProgressTracker.razor     # Real-time progress
├── Data/
│   ├── Models/                       # Data models (POCOs)
│   └── Services/                     # File-based data services
├── Documents/                        # TEST DOCUMENTS FOLDER (Phase 1)
│   ├── sample1.pdf                   # User-provided test documents
│   ├── sample2.jpg                   # 
│   └── ...                           # Additional test files
├── ReviewData/                       # REVIEW RESULTS (JSON files)
│   ├── review_data.json              # Main review data file
│   └── exports/                      # CSV/Excel exports
└── wwwroot/
    └── assets/                       # Static assets
```

---

## 🎯 Core Features & Requirements

### 1. Document Management
#### Document Viewer Component
- **PDF Rendering**: High-quality display with zoom, pan, rotate
- **Image Support**: JPEG, PNG, TIFF for scanned documents
- **Navigation**: Previous/Next with keyboard shortcuts
- **Full Screen**: Distraction-free review mode
- **Document Metadata**: Display current filename, size, last modified

#### File System Integration
- **Solution Documents Folder**: `./Documents/` folder within the project solution
- **Supported Formats**: PDF, JPG, PNG, TIFF
- **Self-Contained**: All test documents included in solution for easy development
- **Auto-Discovery**: Automatically detect files in Documents folder
- **Non-Destructive**: Read-only operations (files stay in place)
- **Future Enhancement**: External folders, S3/Cloud storage integration planned for later phases

**Phase 1 Implementation:**
```csharp
// Simple solution-based document service
public class LocalDocumentService
{
    private readonly string _documentFolder = Path.Combine(
        Directory.GetCurrentDirectory(), 
        "Documents"
    );
    
    public async Task<List<Document>> ScanForDocuments()
    {
        var supportedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff" };
        
        if (!Directory.Exists(_documentFolder))
            Directory.CreateDirectory(_documentFolder);
            
        var files = Directory.GetFiles(_documentFolder, "*.*", SearchOption.AllDirectories)
            .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
            .ToList();
        
        return files.Select(CreateDocumentFromFile).ToList();
    }
}
```

### 2. Voice Capture System (Radzen Components)
#### Voice Input (Radzen Components)
- **RadzenSpeechToTextButton**: Core voice capture component
- **Event-Driven Architecture**: `Change` event handlers for speech capture
- **Text Accumulation**: Append new speech to existing descriptions
- **Real-time Editing**: `RadzenTextArea` for manual correction and editing
- **Activity Logging**: `EventConsole` component for debugging and user feedback
- **Seamless Integration**: Voice input automatically populates editable text area

#### Voice Capture Workflow
1. **Activate Voice**: Click `RadzenSpeechToTextButton` to start recording
2. **Real-time Transcription**: Speech converted to text in real-time
3. **Text Accumulation**: New speech appended to existing description
4. **Manual Editing**: User can edit transcribed text in `RadzenTextArea`
5. **Activity Logging**: All events logged to console for transparency
6. **Save & Continue**: Description saved with document metadata

### 3. Data Management
#### Document Records (JSON Model)
```csharp
public class DocumentReviewData
{
    public List<DocumentReview> Documents { get; set; } = new();
    public ReviewMetadata Metadata { get; set; } = new();
}

public class DocumentReview
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OriginalFilename { get; set; }
    public string FilePath { get; set; }
    public string FileType { get; set; }
    public long FileSize { get; set; }
    public DateTime ImportDate { get; set; }
    public DocumentStatus Status { get; set; }
    public string AssignedReviewer { get; set; }
    
    // Review Data
    public string VoiceTranscription { get; set; }
    public string EditedDescription { get; set; }
    public string Category { get; set; }
    public string Keywords { get; set; }
    public string NewFilename { get; set; }
    
    // Tracking
    public DateTime? ReviewStarted { get; set; }
    public DateTime? ReviewCompleted { get; set; }
    public int ReviewDurationSeconds { get; set; }
    public string ReviewerId { get; set; }
}

public class ReviewMetadata
{
    public DateTime LastUpdated { get; set; }
    public string Version { get; set; } = "1.0";
    public int TotalDocuments { get; set; }
    public int CompletedReviews { get; set; }
    public List<string> Categories { get; set; } = new();
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
```

#### Simple File-Based Data Service
```csharp
public class JsonDataService
{
    private readonly string _dataFile = Path.Combine(Directory.GetCurrentDirectory(), "ReviewData", "review_data.json");
    private DocumentReviewData _cache;

    public async Task<DocumentReviewData> LoadDataAsync()
    {
        if (_cache != null) return _cache;
        
        if (!File.Exists(_dataFile))
        {
            _cache = new DocumentReviewData();
            await SaveDataAsync(_cache);
            return _cache;
        }
        
        var json = await File.ReadAllTextAsync(_dataFile);
        _cache = JsonSerializer.Deserialize<DocumentReviewData>(json) ?? new DocumentReviewData();
        return _cache;
    }
    
    public async Task SaveDataAsync(DocumentReviewData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dataFile));
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_dataFile, json);
        _cache = data;
    }
    
    public async Task SaveDocumentReviewAsync(DocumentReview review)
    {
        var data = await LoadDataAsync();
        var existing = data.Documents.FirstOrDefault(d => d.Id == review.Id);
        
        if (existing != null)
        {
            // Update existing
            var index = data.Documents.IndexOf(existing);
            data.Documents[index] = review;
        }
        else
        {
            // Add new
            data.Documents.Add(review);
        }
        
        data.Metadata.LastUpdated = DateTime.UtcNow;
        data.Metadata.TotalDocuments = data.Documents.Count;
        data.Metadata.CompletedReviews = data.Documents.Count(d => d.Status == DocumentStatus.Completed);
        
        await SaveDataAsync(data);
    }
}
```

#### Categories & Keywords
- **Predefined Categories**: Engineering, Mechanical, Electrical, Process, etc.
- **Dynamic Keywords**: Auto-suggestion based on existing entries
- **Custom Fields**: Extensible metadata schema
- **Validation Rules**: Required fields and format checking

### 4. Multi-User Workflow
#### User Management
- **Simple Authentication**: Local user profiles
- **Role-Based Access**: Reviewer vs. Administrator
- **Session Management**: Resume work across sessions
- **Progress Isolation**: Prevent document conflicts

#### Work Distribution
- **Auto-Assignment**: Round-robin or skill-based distribution
- **Manual Assignment**: Admin override capabilities
- **Workload Balancing**: Visual progress indicators per user
- **Conflict Resolution**: Handle simultaneous review attempts

### 5. Progress Tracking & Reporting
#### Dashboard Features
- **Overall Progress**: Documents completed vs. remaining
- **User Performance**: Individual completion rates and times
- **Quality Metrics**: Review consistency across users
- **Estimated Completion**: Projected timeline based on current pace

#### Export Capabilities
- **CSV Export**: For SharePoint import
- **Excel Reports**: Formatted summaries with charts
- **File Renaming**: Batch rename based on metadata
- **Audit Trail**: Complete review history

---

## 🚀 Implementation Phases (3-Hour Proof of Concept)

### Phase 1: Core Demo (3 Hours Total)
**Hour 1: Basic Setup**
- [ ] Install Radzen.Blazor package
- [ ] Create simple document model (POCO classes)
- [ ] Build basic document browser (scan ./Documents folder)
- [ ] Simple PDF/image viewer (even basic HTML img/embed tag is fine)

**Hour 2: Voice Integration**
- [ ] Add RadzenSpeechToTextButton (using your voice control pattern)
- [ ] RadzenTextArea for editing transcription
- [ ] Basic category dropdown
- [ ] Save to JSON functionality

**Hour 3: Polish & Demo**
- [ ] Next/Previous document navigation
- [ ] Show JSON output (prove it works)
- [ ] Basic export (show the JSON file contents)
- [ ] Clean up UI for demo presentation

**Demo Goals:**
- ✅ Load a few test documents from folder
- ✅ Show voice-to-text working with Radzen
- ✅ Demonstrate text editing capability
- ✅ Save review data to JSON file
- ✅ Show JSON output (ready for upload to central system)
- ✅ Navigate between documents

**NOT in 3-Hour Demo:**
- ❌ Multi-user features
- ❌ Dashboard/reporting
- ❌ Complex PDF rendering
- ❌ Database integration
- ❌ Advanced UI polish
- ❌ Error handling beyond basics

---

## 🔧 Technical Implementation Details

### Radzen Voice Capture Integration
```razor
<!-- Main voice capture interface based on user's voice control example -->
<RadzenRow>
    <RadzenColumn Size="8">
        <!-- Document viewer area -->
        <div class="document-viewer-container">
            <!-- PDF/Image display -->
        </div>
    </RadzenColumn>
    <RadzenColumn Size="4">
        <!-- Voice capture and description area -->
        <RadzenCard>
            <RadzenText TextStyle="TextStyle.H6">Voice Capture</RadzenText>
            
            <!-- Speech to Text Button -->
            <RadzenSpeechToTextButton Change="@(args => OnSpeechCaptured(args, true, "DocumentDescription"))" 
                                      class="rz-mb-3" />
            
            <!-- Editable text area with captured speech -->
            <RadzenTextArea @bind-Value="@documentDescription" 
                           Change="@(args => OnDescriptionChange(args))" 
                           Style="width: 100%; height: 200px" 
                           class="rz-mb-3" 
                           Placeholder="Voice transcription will appear here..."
                           aria-label="Document description" />
            
            <!-- Category and metadata inputs -->
            <RadzenDropDown @bind-Value="@selectedCategory" 
                           Data="@categories" 
                           Placeholder="Select category..." 
                           class="rz-mb-3" />
            
            <RadzenTextBox @bind-Value="@keywords" 
                          Placeholder="Keywords (comma separated)" 
                          class="rz-mb-3" />
            
            <!-- Action buttons -->
            <RadzenButton Text="Save & Next" 
                         Click="@SaveAndNext" 
                         ButtonStyle="ButtonStyle.Success" 
                         class="rz-mr-2" />
            <RadzenButton Text="Skip" 
                         Click="@SkipDocument" 
                         ButtonStyle="ButtonStyle.Secondary" />
        </RadzenCard>
        
        <!-- Event/Activity Log -->
        <EventConsole @ref="@console" class="rz-mt-3" />
    </RadzenColumn>
</RadzenRow>
```

**Code-Behind Implementation:**
```csharp
public partial class DocumentReview : ComponentBase
{
    private string documentDescription = "";
    private string selectedCategory = "";
    private string keywords = "";
    private EventConsole console;
    private List<string> categories = new() { "Engineering", "Mechanical", "Electrical", "Process", "Administrative" };

    void OnSpeechCaptured(string speechValue, bool updateTextArea, string source)
    {
        console.Log($"Speech Captured from {source}: {speechValue}");

        if (updateTextArea)
        {
            // Append new speech to existing description
            if (!string.IsNullOrEmpty(documentDescription))
                documentDescription += " " + speechValue;
            else
                documentDescription = speechValue;
                
            StateHasChanged();
        }
    }

    void OnDescriptionChange(string value)
    {
        console.Log($"Description manually edited: {value?.Length ?? 0} characters");
        documentDescription = value;
    }

    async Task SaveAndNext()
    {
        console.Log($"Saving document with description: {documentDescription}");
        
        // Save logic here
        await SaveCurrentDocument();
        
        // Move to next document
        await LoadNextDocument();
        
        // Clear for next document
        ClearForm();
    }
    
    private void ClearForm()
    {
        documentDescription = "";
        selectedCategory = "";
        keywords = "";
        console.Log("Form cleared for next document");
    }
}
```

### Document Viewer Component
```razor
<div class="document-viewer-container">
    <div class="viewer-toolbar">
        <RadzenButton Icon="zoom_in" Click="@ZoomIn" />
        <RadzenButton Icon="zoom_out" Click="@ZoomOut" />
        <RadzenButton Icon="rotate_right" Click="@RotateRight" />
        <RadzenButton Icon="fullscreen" Click="@ToggleFullscreen" />
    </div>
    
    <div class="pdf-viewer" @ref="pdfContainer">
        <!-- PDF.js integration -->
    </div>
    
    <div class="viewer-navigation">
        <RadzenButton Icon="navigate_before" Click="@PreviousDocument" />
        <span>Document @CurrentIndex of @TotalDocuments</span>
        <RadzenButton Icon="navigate_next" Click="@NextDocument" />
    </div>
</div>
```

### Database Context
```csharp
public class LegacyLabelerContext : DbContext
{
    public DbSet<Document> Documents { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ReviewSession> ReviewSessions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<DocumentKeyword> DocumentKeywords { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Database/legacylabeler.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships and constraints
        modelBuilder.Entity<Document>()
            .HasMany<DocumentKeyword>()
            .WithOne()
            .HasForeignKey(dk => dk.DocumentId);
    }
}
```

---

## 📊 Database Design

### Core Tables
```sql
-- Documents
Documents (
    Id INTEGER PRIMARY KEY,
    OriginalFilename TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    FileType TEXT NOT NULL,
    FileSize INTEGER,
    ImportDate DATETIME,
    Status INTEGER,
    AssignedReviewer TEXT,
    VoiceTranscription TEXT,
    ReviewedDescription TEXT,
    Category TEXT,
    Keywords TEXT,
    NewFilename TEXT,
    ReviewStarted DATETIME,
    ReviewCompleted DATETIME,
    ReviewDuration INTEGER,
    ReviewerId TEXT
)

-- Users
Users (
    Id TEXT PRIMARY KEY,
    Username TEXT NOT NULL,
    DisplayName TEXT,
    Role TEXT,
    IsActive BOOLEAN,
    CreatedDate DATETIME
)

-- Review Sessions
ReviewSessions (
    Id INTEGER PRIMARY KEY,
    UserId TEXT,
    DocumentId INTEGER,
    StartTime DATETIME,
    EndTime DATETIME,
    TranscriptionText TEXT,
    FinalDescription TEXT,
    SessionNotes TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (DocumentId) REFERENCES Documents(Id)
)
```

---

## 🎨 User Interface Design

### Main Review Interface Layout
```
┌─────────────────────────────────────────────────────────────────┐
│ Legacy Labeler - Document Review                               │
├─────────────────────────────────────────────────────────────────┤
│ [◀ Prev]  Document 247 of 2567  [Next ▶]  [Dashboard] [Export] │
├─────────────────────────────────────────────────────────────────┤
│                                    │                            │
│                                    │ 🎤 Voice Capture           │
│        Document Viewer             │ [🎤 Start Recording]       │
│                                    │                            │
│     [PDF/Image Display Area]       │ Transcription:             │
│                                    │ ┌────────────────────────┐ │
│                                    │ │ [Live transcribed text]│ │
│                                    │ │                        │ │
│                                    │ │                        │ │
│                                    │ └────────────────────────┘ │
│                                    │                            │
│     [🔍 Zoom] [↻ Rotate] [⛶ Full] │ Category: [Dropdown ▼]    │
│                                    │ Keywords: [Input Field]   │
│                                    │                            │
│                                    │ [Save & Next] [Skip]       │
└────────────────────────────────────┴────────────────────────────┘
```

### Dashboard Interface
```
┌─────────────────────────────────────────────────────────────────┐
│ Dashboard - Progress Overview                                   │
├─────────────────────────────────────────────────────────────────┤
│ Overall Progress: ████████████░░░░  67% (1,719/2,567)         │
│                                                                 │
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐│
│ │ Completed   │ │ In Progress │ │ Pending     │ │ Avg Time    ││
│ │    1,719    │ │      23     │ │     825     │ │  1.2 min    ││
│ └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘│
│                                                                 │
│ User Performance:                                               │
│ ┌─────────────────────────────────────────────────────────────┐│
│ │ User          │ Completed │ Avg Time │ Quality Score │ Today ││
│ │ Mike Thompson │    456    │ 1.1 min  │     95%       │  12   ││
│ │ Sarah Chen    │    389    │ 1.3 min  │     92%       │   8   ││
│ │ Bob Johnson   │    297    │ 1.0 min  │     97%       │  15   ││
│ └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔒 Security & Data Protection

### Data Security Requirements
- **Local Storage Only**: No external API calls or cloud storage
- **File System Security**: Read-only access to source documents
- **Database Encryption**: SQLite encryption for sensitive metadata
- **Audit Trail**: Complete logging of all review actions
- **Backup Strategy**: Regular automated backups of database and metadata

### Access Control
- **User Authentication**: Simple local user management
- **Role-Based Permissions**: Admin vs. Reviewer capabilities
- **Session Security**: Automatic logout after inactivity
- **Data Isolation**: Users only see assigned documents

---

## 📝 Development Notes

### Current Status
- ✅ Basic Blazor Server application structure created
- ✅ Navigation and layout components implemented
- ✅ Code-behind pattern established
- ⏳ Radzen components integration pending
- ⏳ Voice capture implementation pending

### Technical Decisions Made
1. **Blazor Server** over Desktop App - Better for rapid development and easier deployment
2. **Radzen Components** for voice capture - Using `RadzenSpeechToTextButton` with `Change` event pattern
3. **Event-Driven Voice Architecture** - Speech accumulation with manual editing capabilities
4. **JSON File Storage** - Simple, human-readable, no database complexity for Phase 1
5. **Code-Behind Pattern** - Separation of concerns, better maintainability
6. **EventConsole Integration** - Activity logging for transparency and debugging

### Technical Decisions Pending
1. **PDF Rendering Library** - PDF.js vs. PDFium vs. other options
2. **Voice Processing** - Specific Radzen voice component configuration
3. **File Organization** - How to handle large document collections efficiently
4. **Deployment Strategy** - Self-contained vs. framework-dependent
5. **Database Migration** - When and how to move from JSON to database (if needed)

### Risks & Mitigation
| Risk | Impact | Mitigation |
|------|--------|------------|
| Voice accuracy issues | High | Implement easy correction UI, allow manual text entry |
| Large file performance | Medium | Implement lazy loading, thumbnail generation |
| User adoption resistance | High | Focus on simple UX, provide comprehensive training |
| JSON file corruption | Low | Regular backups, file versioning, atomic writes |
| Large dataset performance | Medium | Consider database migration when dataset grows |

### Future Enhancements
- **Database Migration**: Move to SQLite/PostgreSQL when dataset becomes large
- **Cloud Storage Integration**: S3, Azure Blob, SharePoint document libraries
- AI-powered text normalization (Phase 2)
- Advanced search capabilities
- Integration with SharePoint API
- Mobile tablet support for field work
- Batch processing automation
- **Advanced File Sources**: Network drives, FTP, cloud storage APIs

---

## 📋 Action Items & Next Steps

### Immediate Actions (This Week)
- [ ] Install Radzen Blazor components package
- [ ] Create basic document model and database context
- [ ] Implement simple document browser with file system access
- [ ] Set up basic PDF viewer component
- [ ] Test Radzen voice capture component integration

### Short Term (Next 2 Weeks)
- [ ] Complete Phase 1 deliverables
- [ ] Begin Phase 2 voice integration
- [ ] Set up development environment for team members
- [ ] Create initial test document set for development

### Medium Term (Next Month)
- [ ] Complete core review functionality (Phases 1-3)
- [ ] Begin user testing with SME team
- [ ] Refine UI based on feedback
- [ ] Optimize performance for large document sets

---

## 💭 Meeting Notes & Decisions

### 2026-01-14 - Project Kickoff & Scope Clarification
- **Decision**: Use Radzen controls for voice capture functionality
- **Decision**: Implement code-behind pattern for all components
- **Decision**: **Phase 1 Simplification** - Read documents from `./Documents/` folder within solution
- **Decision**: **Single JSON File Storage** - Perfect for 3-hour demo and future central upload
- **CRITICAL**: **3-Hour Proof-of-Concept Timeline** - Build quick demo to show organization
- **Future Strategy**: JSON files can be uploaded to central system, enabling crowd-sourced reviews
- **Note**: User has existing Radzen voice capture example to reference
- **Note**: Focus on core workflow demo, not production features
- **Next**: Build minimal viable demo in 3 hours

### Future Meeting Notes
*Meeting notes and decisions will be added here as the project progresses*

---

**Document Version**: 1.0  
**Last Updated**: January 14, 2026  
**Next Review**: Weekly project meetings