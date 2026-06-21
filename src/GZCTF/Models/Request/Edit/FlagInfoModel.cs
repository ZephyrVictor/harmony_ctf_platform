namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Flag information (Edit)
/// </summary>
public class FlagInfoModel
{
    /// <summary>
    /// Flag Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Flag text
    /// </summary>
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// Level of this flag (1, 2, 3...)
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// Track Id of this flag
    /// </summary>
    public Guid TrackId { get; set; }

    /// <summary>
    /// Attachment corresponding to the Flag
    /// </summary>
    public Attachment? Attachment { get; set; }

    internal static FlagInfoModel FromChallengeFlag(ChallengeFlag flag) =>
        new()
        {
            Id = flag.Id,
            Flag = flag.FlagValue,
            Level = flag.Level,
            TrackId = flag.TrackId,
            Attachment = flag.Attachment
        };
}
