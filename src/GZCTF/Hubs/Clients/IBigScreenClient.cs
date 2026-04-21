namespace GZCTF.Hubs.Clients;

public interface IBigScreenClient
{
    /// <summary>
    /// Receive a big screen event in real-time
    /// </summary>
    Task ReceiveEvent(BigScreenEventDto evt);
}

public class BigScreenEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public string? ChallengeName { get; set; }
    public int? ScoreChange { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
