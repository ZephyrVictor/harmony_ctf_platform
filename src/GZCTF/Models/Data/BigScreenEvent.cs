using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(IsConsumed), nameof(CreatedAt))]
public class BigScreenEvent
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Event type
    /// </summary>
    public BigScreenEventType EventType { get; set; }

    /// <summary>
    /// Display message
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Team name for display
    /// </summary>
    [MaxLength(50)]
    public string? TeamName { get; set; }

    /// <summary>
    /// Challenge name for display
    /// </summary>
    [MaxLength(100)]
    public string? ChallengeName { get; set; }

    /// <summary>
    /// Related score change
    /// </summary>
    public int? ScoreChange { get; set; }

    /// <summary>
    /// Whether this event has been consumed by the big screen
    /// </summary>
    public bool IsConsumed { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    #region Db Relationship

    /// <summary>
    /// Challenge ID
    /// </summary>
    public int ChallengeId { get; set; }

    /// <summary>
    /// Challenge
    /// </summary>
    public GameChallenge? Challenge { get; set; }

    /// <summary>
    /// Related user ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Related user
    /// </summary>
    public UserInfo? User { get; set; }

    #endregion Db Relationship
}

[JsonConverter(typeof(JsonStringEnumConverter<BigScreenEventType>))]
public enum BigScreenEventType
{
    Bonus
}
