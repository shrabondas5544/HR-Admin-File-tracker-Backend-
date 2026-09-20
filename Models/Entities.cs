namespace CabinetMap.Api.Models;

public class Cabinet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "Cabinet 1"
    public int CabinetNumber { get; set; } // 1 to 6
    public string Description { get; set; } = string.Empty;

    public List<Shelf> Shelves { get; set; } = new();
}

public class Shelf
{
    public int Id { get; set; }
    public int CabinetId { get; set; }
    public Cabinet? Cabinet { get; set; }

    public string Section { get; set; } = "Upper"; // "Upper" or "Lower"
    public string ShelfCode { get; set; } = string.Empty; // U1, U2, U3, U4 or L1, L2, L3
    public int OrderIndex { get; set; } // 1, 2, 3, 4 (top to bottom)

    public List<Magazine> Magazines { get; set; } = new();
    public List<Folder> Folders { get; set; } = new();
    public List<RecordFile> StandaloneFiles { get; set; } = new();
}

public class Magazine
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // e.g. "MAG-HR-01"
    public string ColorHex { get; set; } = "#3B82F6"; // Blue default
    public int? ShelfId { get; set; }
    public Shelf? Shelf { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Trash & Soft Delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public List<RecordFile> Files { get; set; } = new();
}

public class Folder
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // e.g. "FLD-BLL-2025"
    public string ColorHex { get; set; } = "#10B981"; // Emerald default
    public int? ShelfId { get; set; }
    public Shelf? Shelf { get; set; }
    public int OrderIndex { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public string AttachmentsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Trash & Soft Delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}

public class RecordFile
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // e.g. "TEL-EMP-1049"
    public string Title { get; set; } = string.Empty; // e.g. "Rakibul Hasan - TEL Service Record"
    
    public int? DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }

    // Dynamic metadata stored as JSON
    public string MetadataJson { get; set; } = "{}";

    public int? MagazineId { get; set; }
    public Magazine? Magazine { get; set; }

    public int? ShelfId { get; set; }
    public Shelf? Shelf { get; set; }

    public int OrderIndex { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public string AttachmentsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Trash & Soft Delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}

public class DocumentType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // "TEL", "BLL", "Custom"
    public string Description { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; } = false;

    public string FieldsJson { get; set; } = "[]";
}
