namespace DisclosureEngine.Application.DTOs;

public sealed record AttachmentResponse(
    Guid Id,
    Guid ReportId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);
