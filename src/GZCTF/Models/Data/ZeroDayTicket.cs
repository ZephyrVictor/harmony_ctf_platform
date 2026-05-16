using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(UserId))]
[Index(nameof(Status))]
[Index(nameof(GameId))]
public class ZeroDayTicket
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Title { get; set; }

    public ZeroDayTicketStatus Status { get; set; } = ZeroDayTicketStatus.Pending;

    [MaxLength(500)]
    public string? ReviewComment { get; set; }

    public int BonusScore { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }

    #region Db Relationship

    public int GameId { get; set; }
    public Game? Game { get; set; }

    public int? ChallengeId { get; set; }
    public GameChallenge? Challenge { get; set; }

    public Guid UserId { get; set; }
    public UserInfo? User { get; set; }

    public Guid? ReviewerId { get; set; }
    public UserInfo? Reviewer { get; set; }

    #endregion Db Relationship
}

[JsonConverter(typeof(JsonStringEnumConverter<ZeroDayTicketStatus>))]
public enum ZeroDayTicketStatus
{
    Pending,
    Approved,
    Rejected
}
