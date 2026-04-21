using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(Key), IsUnique = true)]
public class CompetitionSetting
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Setting key (e.g., "Scoring.MinScore")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Setting value as JSON string
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
