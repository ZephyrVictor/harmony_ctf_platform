using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(TicketId))]
[Index(nameof(LocalFileId))]
public class TicketAttachment
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    #region Db Relationship

    public Guid TicketId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public int? LocalFileId { get; set; }

    public LocalFile? LocalFile { get; set; }

    public Guid? UploaderId { get; set; }

    public UserInfo? Uploader { get; set; }

    #endregion Db Relationship
}
