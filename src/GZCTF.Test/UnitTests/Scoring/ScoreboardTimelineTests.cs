using System;
using System.Collections.Generic;
using System.Linq;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories;
using Xunit;

namespace GZCTF.Test.UnitTests.Scoring;

public class ScoreboardTimelineTests
{
    [Fact]
    public void DownsampleTimeline_UnderLimit_ReturnsOriginalTimeline()
    {
        var timeline = CreateTimeline(10);

        var result = GameRepository.DownsampleTimeline(timeline, 10);

        Assert.Same(timeline, result);
    }

    [Fact]
    public void DownsampleTimeline_OverLimit_PreservesOrderEndpointsAndExtremes()
    {
        const int maxPoints = 2000;
        var timeline = CreateTimeline(5000);
        timeline[2345].Score = 100_000;
        timeline[3456].Score = -100_000;

        var result = GameRepository.DownsampleTimeline(timeline, maxPoints);

        Assert.True(result.Count <= maxPoints);
        Assert.Same(timeline[0], result[0]);
        Assert.Same(timeline[^1], result[^1]);
        Assert.Contains(result, point => point.Score == 100_000 && point.Time == timeline[2345].Time);
        Assert.Contains(result, point => point.Score == -100_000 && point.Time == timeline[3456].Time);
        Assert.True(result.Zip(result.Skip(1), (left, right) => left.Time < right.Time).All(isOrdered => isOrdered));
    }

    private static List<TimeLine> CreateTimeline(int count)
    {
        var start = DateTimeOffset.UtcNow;
        return Enumerable.Range(0, count)
            .Select(index => new TimeLine
            {
                Time = start.AddSeconds(index),
                Score = index % 300
            })
            .ToList();
    }
}
