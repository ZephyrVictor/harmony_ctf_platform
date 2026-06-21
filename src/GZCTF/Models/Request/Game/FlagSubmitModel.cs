using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Flag submission
/// </summary>
public class FlagSubmitModel
{
    /// <summary>
    /// Flag content
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_FlagRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// Track Id (Level 1 入口可为空；Level ≥2 必填)
    /// </summary>
    public Guid? TrackId { get; set; }

    /// <summary>
    /// Level (1, 2, 3...)
    /// </summary>
    public int Level { get; set; } = 1;
}
