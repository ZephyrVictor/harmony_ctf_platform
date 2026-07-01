namespace GZCTF.Models.Response;

/// <summary>
/// 封禁状态 DTO
/// </summary>
public class BanStateDto
{
    public int Id { get; set; }
    public int ParticipationId { get; set; }
    public int TeamId { get; set; }
    public int ChallengeId { get; set; }
    public Guid TrackId { get; set; }
    public int Level { get; set; }
    public string BanAnswer { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public Guid? BannedBy { get; set; }
    public bool IsManual { get; set; }
    public DateTimeOffset BannedAt { get; set; }
}
