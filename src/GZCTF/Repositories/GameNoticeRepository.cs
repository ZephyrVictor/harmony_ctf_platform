using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace GZCTF.Repositories;

public class GameNoticeRepository(
    CacheHelper cacheHelper,
    ILogger<GameNoticeRepository> logger,
    IHubContext<UserHub, IUserClient> hub,
    AppDbContext context) : RepositoryBase(context), IGameNoticeRepository
{
    public async Task<GameNotice> AddNotice(GameNotice notice, CancellationToken token = default)
    {
        await Context.AddAsync(notice, token);
        await SaveAsync(token);

        await cacheHelper.RemoveAsync(CacheKey.GameNotice(notice.GameId), token);

        await hub.Clients.Group($"Game_{notice.GameId}").ReceivedGameNotice(notice);

        return notice;
    }

    public Task<GameNotice[]> GetNormalNotices(int gameId, CancellationToken token = default) =>
        Context.GameNotices
            .Where(n => n.GameId == gameId && n.Type == NoticeType.Normal)
            .ToArrayAsync(token);

    public Task<GameNotice?> GetNoticeById(int gameId, int noticeId, CancellationToken token = default) =>
        Context.GameNotices.FirstOrDefaultAsync(e => e.Id == noticeId && e.GameId == gameId, token);

    public Task<DataWithModifiedTime<GameNotice[]>> GetLatestNotices(int gameId, CancellationToken token = default)
        => cacheHelper.GetOrCreateAsync(logger, CacheKey.GameNotice(gameId), async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            // player-facing：排除 admin-only 类型（RateLimitAlert）
            var notices = await Context.GameNotices
                .Where(e => e.GameId == gameId && e.Type != NoticeType.RateLimitAlert)
                .OrderByDescending(e => e.Type == NoticeType.Normal ? DateTimeOffset.UtcNow : e.PublishTimeUtc)
                .Take(300).ToArrayAsync(token);
            return new DataWithModifiedTime<GameNotice[]>(notices, DateTimeOffset.UtcNow);
        }, token: token);

    /// <summary>
    /// 获取告警类通知（admin-only，含 RateLimitAlert）
    /// </summary>
    public Task<GameNotice[]> GetAlertNotices(int gameId, CancellationToken token = default) =>
        Context.GameNotices
            .Where(n => n.GameId == gameId && n.Type == NoticeType.RateLimitAlert)
            .OrderByDescending(n => n.PublishTimeUtc)
            .Take(200)
            .ToArrayAsync(token);

    public async Task RemoveNotice(GameNotice notice, CancellationToken token = default)
    {
        Context.Remove(notice);
        await SaveAsync(token);

        await cacheHelper.RemoveAsync(CacheKey.GameNotice(notice.GameId), token);
    }

    public async Task<GameNotice> UpdateNotice(GameNotice notice, CancellationToken token = default)
    {
        notice.PublishTimeUtc = DateTimeOffset.UtcNow;
        await SaveAsync(token);

        await cacheHelper.RemoveAsync(CacheKey.GameNotice(notice.GameId), token);

        return notice;
    }
}
