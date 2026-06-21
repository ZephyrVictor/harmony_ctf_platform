using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Flag submission response (D6)
/// </summary>
public class FlagSubmitResponse
{
    /// <summary>
    /// Submission ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Result message (e.g., "Accepted", "Wrong answer", "Trap answer triggered")
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Matched Track ID (D6: L1 response returns matched trackId for L2+ submissions)
    /// </summary>
    public Guid? TrackId { get; set; }

    /// <summary>
    /// Matched Level (D6: current level that was submitted)
    /// </summary>
    public int? Level { get; set; }
}
