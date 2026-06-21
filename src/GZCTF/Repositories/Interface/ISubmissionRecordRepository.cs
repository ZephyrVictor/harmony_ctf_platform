namespace GZCTF.Repositories.Interface;

public interface ISubmissionRecordRepository : IRepository
{
    Task<SubmissionRecord> CreateRecord(SubmissionRecord record, CancellationToken token = default);
    Task<SubmissionRecord?> GetBySubmissionId(int submissionId, CancellationToken token = default);
    Task<bool> ExistsBySubmissionId(int submissionId, CancellationToken token = default);
    Task<SubmissionRecord[]> GetByUser(Guid userId, int? challengeId = null, int? gameId = null, int count = 100,
        int skip = 0, CancellationToken token = default);
    Task<bool> HasCorrectSubmission(int challengeId, int participationId, Guid flagId, CancellationToken token = default);
    Task<bool> HasCorrectSubmission(int challengeId, int participationId, Guid trackId, int level, CancellationToken token = default);
    Task<Guid[]> GetSolvedFlagIds(int challengeId, int participationId, CancellationToken token = default);
    Task<List<(Guid TrackId, int Level, DateTimeOffset SolvedAt)>> GetSolvedTrackLevels(int challengeId, int participationId, CancellationToken token = default);

    /// <summary>
    /// Count all submission records (correct + wrong) for a specific (trackId, level).
    /// Used to populate <c>TrackLevelStatusModel.AttemptsUsed</c> with the true
    /// total attempt count, not just wrong submissions.
    /// </summary>
    Task<int> CountSubmissionsByTrackLevel(int challengeId, int participationId, Guid trackId, int level, CancellationToken token = default);
    Task<int> GetTotalScoreChange(int participationId, CancellationToken token = default);
    Task<int> GetGZCTFBaseScore(int participationId, int gameId, CancellationToken token = default);
}

