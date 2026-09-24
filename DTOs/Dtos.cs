namespace CabinetMap.Api.DTOs;

public class CreateFileDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int? DocumentTypeId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public int? MagazineId { get; set; }
    public int? ShelfId { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentsJson { get; set; }
}

public class UpdateFileDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int? DocumentTypeId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public int? MagazineId { get; set; }
    public int? ShelfId { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentsJson { get; set; }
}

public class MoveItemDto
{
    public int? TargetShelfId { get; set; }
    public int? TargetMagazineId { get; set; }
    public int? OrderIndex { get; set; }
}

public class CreateMagazineDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#3B82F6";
    public int ShelfId { get; set; }
}

public class UpdateMagazineDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#3B82F6";
    public int? ShelfId { get; set; }
}

public class CreateFolderDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#10B981";
    public int ShelfId { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentsJson { get; set; }
}

public class UpdateFolderDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#10B981";
    public int? ShelfId { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentsJson { get; set; }
}

public class CreateDocumentTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FieldsJson { get; set; } = "[]";
}

public class SearchResultDto
{
    public string Type { get; set; } = string.Empty; // "File", "Magazine", "Folder"
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public int CabinetNumber { get; set; }
    public string CabinetName { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string ShelfCode { get; set; } = string.Empty;
    public int ShelfId { get; set; }
    public int? MagazineId { get; set; }
    public string? MagazineName { get; set; }
    public string? ColorHex { get; set; }
    public string? HighlightField { get; set; }
}

public class TrashItemDto
{
    public string Type { get; set; } = string.Empty; // "File", "Magazine", "Folder"
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int DaysRemaining { get; set; }
    public string OriginalLocation { get; set; } = string.Empty;
    public int? FileCount { get; set; }
}

public class ReorderItemDto
{
    public string Type { get; set; } = string.Empty; // "Magazine", "Folder", "File"
    public int Id { get; set; }
    public int OrderIndex { get; set; }
}

public class ReorderShelfDto
{
    public List<ReorderItemDto> Items { get; set; } = new();
}
