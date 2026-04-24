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
    /// Score contribution of this flag
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Attachment corresponding to the Flag
    /// </summary>
    public Attachment? Attachment { get; set; }

    internal static FlagInfoModel FromChallengeFlag(ChallengeFlag flag) =>
        new() { Id = flag.Id, Flag = flag.FlagValue, Score = flag.Score, Attachment = flag.Attachment };
}
