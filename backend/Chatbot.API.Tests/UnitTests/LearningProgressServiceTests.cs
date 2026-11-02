using Chatbot.API.DTOs.ChildProfile;
using Chatbot.API.DTOs.LearningProgress;
using Chatbot.API.Entities;
using Chatbot.API.Exceptions;
using Chatbot.API.Services;
using Chatbot.API.Tests.Helpers;
using Xunit;

namespace Chatbot.API.Tests.UnitTests;

public class LearningProgressServiceTests
{
    [Theory]
    [InlineData(25.0, MasteryStatus.Novice)]
    [InlineData(55.0, MasteryStatus.Developing)]
    [InlineData(80.0, MasteryStatus.Proficient)]
    [InlineData(95.0, MasteryStatus.Mastered)]
    public async Task RecordProgressAsync_SetsCorrectMasteryStatus(double score, MasteryStatus expectedStatus)
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var profileService = new ChildProfileService(db);
        var progressService = new LearningProgressService(db);

        var child = await profileService.CreateAsync(new CreateChildProfileRequest(
            "Emma", null, new DateOnly(2019, 4, 1), 1, "en", null, "emma@test.com"));

        var request = new RecordProgressRequest(
            Topic: "Phonics",
            ScoreDeltaOrValue: score,
            IsIncremental: false,
            ActivitiesCompleted: 3,
            TimeSpentMinutes: 25,
            Notes: "Completed letter sounds"
        );

        // Act
        var result = await progressService.RecordProgressAsync(child.Id, request);

        // Assert
        Assert.Equal(score, result.ProficiencyScore);
        Assert.Equal(expectedStatus, result.MasteryStatus);
        Assert.Equal(3, result.CompletedActivitiesCount);
        Assert.Equal(25, result.TotalTimeSpentMinutes);
    }

    [Fact]
    public async Task RecordProgressAsync_Incremental_AccumulatesScoresAndCounts()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var profileService = new ChildProfileService(db);
        var progressService = new LearningProgressService(db);

        var child = await profileService.CreateAsync(new CreateChildProfileRequest(
            "Liam", null, new DateOnly(2018, 9, 12), 2, "en", null, "liam@test.com"));

        // First session: 30 points, 2 activities, 15 min
        await progressService.RecordProgressAsync(child.Id, new RecordProgressRequest(
            "Math", 30.0, false, 2, 15, "Session 1"));

        // Second session: incremental +20 points, 1 activity, 10 min
        var result = await progressService.RecordProgressAsync(child.Id, new RecordProgressRequest(
            "Math", 20.0, true, 1, 10, "Session 2"));

        // Assert
        Assert.Equal(50.0, result.ProficiencyScore);
        Assert.Equal(3, result.CompletedActivitiesCount);
        Assert.Equal(25, result.TotalTimeSpentMinutes);
        Assert.Equal(MasteryStatus.Developing, result.MasteryStatus);
    }

    [Fact]
    public async Task GetSummaryAsync_ComputesAccurateAggregations()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var profileService = new ChildProfileService(db);
        var progressService = new LearningProgressService(db);

        var child = await profileService.CreateAsync(new CreateChildProfileRequest(
            "Lucas", null, new DateOnly(2017, 2, 14), 2, "en", null, "lucas@test.com"));

        await progressService.RecordProgressAsync(child.Id, new RecordProgressRequest("Math", 80.0, false, 4, 30, null));
        await progressService.RecordProgressAsync(child.Id, new RecordProgressRequest("Reading", 60.0, false, 2, 20, null));

        // Act
        var summary = await progressService.GetSummaryAsync(child.Id);

        // Assert
        Assert.Equal(2, summary.TotalTopicsTracked);
        Assert.Equal(70.0, summary.AverageProficiency); // (80 + 60) / 2
        Assert.Equal(6, summary.TotalActivitiesCompleted);
        Assert.Equal(50, summary.TotalTimeSpentMinutes);
    }

    [Fact]
    public async Task RecordProgressAsync_NonExistentChild_ThrowsNotFoundException()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var progressService = new LearningProgressService(db);

        var request = new RecordProgressRequest("Science", 50, false, 1, 10, null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => progressService.RecordProgressAsync(Guid.NewGuid(), request));
    }
}
