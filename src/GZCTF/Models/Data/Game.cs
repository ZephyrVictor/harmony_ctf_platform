using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using GZCTF.Models.Request.Edit;
using MemoryPack;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;

namespace GZCTF.Models.Data;

[MemoryPackable]
public partial class Game
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Game title
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Token signature public key
    /// </summary>
    [Required]
    [MaxLength(Limits.GameKeyLength)]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Token signature private key
    /// </summary>
    [Required]
    [MaxLength(Limits.GameKeyLength)]
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether to hide
    /// </summary>
    [Required]
    public bool Hidden { get; set; }

    /// <summary>
    /// Whether the game is in practice mode (most operations can still be performed after the game ends)
    /// </summary>
    public bool PracticeMode { get; set; } = true;

    /// <summary>
    /// Poster hash
    /// </summary>
    [MaxLength(Limits.FileHashLength)]
    public string? PosterHash { get; set; }

    /// <summary>
    /// Game description
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Detailed introduction of the game
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 0day 提交说明 markdown 文案（管理员可编辑，前端 ZeroDayInfo tab 渲染）
    /// </summary>
    [MaxLength(8192)]
    public string ZeroDayInfoMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// 0day 加分基础倍数（Critical = base×100, High = base×60, Medium = base×30, Low = base×10）
    /// </summary>
    public int ZeroDayBaseScore { get; set; } = 1;

    /// <summary>
    /// 题目冷却分钟数。答错达阈值（trackCount×5）后封锁该题 X 分钟，自动解封。0 = 不限流
    /// </summary>
    public int CooldownMinutes { get; set; } = 5;

    /// <summary>
    /// Teams can join without review
    /// </summary>
    public bool AcceptWithoutReview { get; set; }

    /// <summary>
    /// Whether writeup is required
    /// </summary>
    public bool WriteupRequired { get; set; }

    /// <summary>
    /// Game invitation code
    /// </summary>
    [MaxLength(Limits.InviteTokenLength)]
    public string? InviteCode { get; set; }

    /// <summary>
    /// Limit on the number of team members, 0 means no limit
    /// </summary>
    public int TeamMemberCountLimit { get; set; }

    /// <summary>
    /// Limit on the number of containers a team can have simultaneously
    /// </summary>
    public int ContainerCountLimit { get; set; } = 3;

    /// <summary>
    /// Start time
    /// </summary>
    [Required]
    [JsonPropertyName("start")]
    public DateTimeOffset StartTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// End time
    /// </summary>
    [Required]
    [JsonPropertyName("end")]
    public DateTimeOffset EndTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// Writeup submission deadline
    /// </summary>
    [Required]
    public DateTimeOffset WriteupDeadline { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// Additional notes for writeup
    /// </summary>
    [Required]
    public string WriteupNote { get; set; } = string.Empty;

    [JsonIgnore]
    [Column(nameof(BloodBonus))]
    public long BloodBonusValue { get; set; } = BloodBonus.DefaultValue;

    /// <summary>
    /// Blood bonus
    /// </summary>
    [NotMapped]
    [Required]
    [MemoryPackIgnore]
    public BloodBonus BloodBonus
    {
        get => BloodBonus.FromValue(BloodBonusValue);
        set => BloodBonusValue = value.Val;
    }

    /// <summary>
    /// Whether the game is active
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    [MemoryPackIgnore]
    public bool IsActive => StartTimeUtc <= DateTimeOffset.Now && DateTimeOffset.Now <= EndTimeUtc;

    /// <summary>
    /// Poster URL
    /// </summary>
    [NotMapped]
    [MemoryPackIgnore]
    public string? PosterUrl => GetPosterUrl(PosterHash);

    /// <summary>
    /// Team hash salt
    /// </summary>
    [NotMapped]
    [MemoryPackIgnore]
    public string TeamHashSalt => $"GZCTF@{PrivateKey}@PK".ToSHA256String();

    internal static string? GetPosterUrl(string? hash) => hash is null ? null : PathHelper.PrefixUrl($"/assets/{hash}/poster");

    internal void GenerateKeyPair(byte[]? xorKey)
    {
        SecureRandom sr = new();
        Ed25519KeyPairGenerator kpg = new();
        kpg.Init(new Ed25519KeyGenerationParameters(sr));
        var kp = kpg.GenerateKeyPair();
        var privateKey = (Ed25519PrivateKeyParameters)kp.Private;
        var publicKey = (Ed25519PublicKeyParameters)kp.Public;

        PrivateKey =
            Base64.ToBase64String(xorKey is null
                ? privateKey.GetEncoded()
                : Codec.Xor(privateKey.GetEncoded(), xorKey));

        PublicKey = Base64.ToBase64String(publicKey.GetEncoded());
    }

    internal string Sign(string str, byte[]? xorKey)
    {
        Ed25519PrivateKeyParameters privateKey;
        if (xorKey is null)
            privateKey = new(Codec.Base64.DecodeToBytes(PrivateKey), 0);
        else
            privateKey = new(Codec.Xor(Codec.Base64.DecodeToBytes(PrivateKey), xorKey), 0);

        return CryptoUtils.GenerateSignature(str, privateKey, SignAlgorithm.Ed25519);
    }

    internal Game Update(GameInfoModel model)
    {
        Title = model.Title;
        Content = model.Content;
        Summary = model.Summary;
        Hidden = model.Hidden;
        PracticeMode = model.PracticeMode;
        AcceptWithoutReview = model.AcceptWithoutReview;
        InviteCode = model.InviteCode;
        EndTimeUtc = model.EndTimeUtc;
        StartTimeUtc = model.StartTimeUtc;
        TeamMemberCountLimit = model.TeamMemberCountLimit;
        ContainerCountLimit = model.ContainerCountLimit;
        WriteupNote = model.WriteupNote;
        WriteupRequired = model.WriteupRequired;
        WriteupDeadline = model.WriteupDeadline;
        BloodBonus = BloodBonus.FromValue(model.BloodBonusValue);
        ZeroDayInfoMarkdown = model.ZeroDayInfoMarkdown;
        ZeroDayBaseScore = model.ZeroDayBaseScore;
        CooldownMinutes = model.CooldownMinutes;

        return this;
    }

    #region Db Relationship

    /// <summary>
    /// Game events
    /// </summary>
    [JsonIgnore]
    public List<GameEvent> GameEvents { get; set; } = [];

    /// <summary>
    /// Game notices
    /// </summary>
    [JsonIgnore]
    public List<GameNotice> GameNotices { get; set; } = [];

    /// <summary>
    /// Game submissions
    /// </summary>
    [JsonIgnore]
    public List<Submission> Submissions { get; set; } = [];

    /// <summary>
    /// Game challenges
    /// </summary>
    [JsonIgnore]
    public HashSet<GameChallenge> Challenges { get; set; } = [];

    /// <summary>
    /// Game participations
    /// </summary>
    [JsonIgnore]
    public HashSet<Participation> Participations { get; set; } = [];

    /// <summary>
    /// Game teams
    /// </summary>
    [JsonIgnore]
    public HashSet<Team>? Teams { get; set; }

    /// <summary>
    /// List of divisions for the game
    /// </summary>
    public HashSet<Division>? Divisions { get; set; }

    #endregion Db Relationship
}
