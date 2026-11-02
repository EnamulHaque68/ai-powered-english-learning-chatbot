using Chatbot.API.DTOs.ChildProfile;
using Chatbot.API.Exceptions;
using Chatbot.API.Services;
using Chatbot.API.Tests.Helpers;
using Xunit;

namespace Chatbot.API.Tests.UnitTests;

public class ChildProfileServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesChildProfile()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var service = new ChildProfileService(db);
        var request = new CreateChildProfileRequest(
            FirstName: "Leo",
            LastName: "Smith",
            DateOfBirth: new DateOnly(2020, 5, 15),
            CurrentLevelId: 1,
            PreferredLanguage: "en",
            Interests: "Dinosaurs, Blocks",
            ParentEmail: "parent@example.com"
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Leo", result.FirstName);
        Assert.Equal("Leo Smith", result.FullName);
        Assert.Equal(1, result.CurrentLevelId);
        Assert.Equal("Toddler Explorer", result.LevelTitle);
        Assert.True(result.Age > 0);
    }

    [Fact]
    public async Task CreateAsync_InvalidLevelId_ThrowsNotFoundException()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var service = new ChildProfileService(db);
        var request = new CreateChildProfileRequest(
            FirstName: "Maya",
            LastName: null,
            DateOfBirth: new DateOnly(2019, 3, 10),
            CurrentLevelId: 9999, // Non-existent
            PreferredLanguage: "en",
            Interests: "Art",
            ParentEmail: "maya.mom@example.com"
        );

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var service = new ChildProfileService(db);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesProfileDetails()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var service = new ChildProfileService(db);
        var created = await service.CreateAsync(new CreateChildProfileRequest(
            "Sophia", null, new DateOnly(2018, 1, 1), 1, "en", "Drawing", "sophia@test.com"));

        var updateRequest = new UpdateChildProfileRequest(
            "Sophia", "Johnson", new DateOnly(2018, 1, 1), 2, "es", "Drawing & Math", "sophia.updated@test.com");

        // Act
        var updated = await service.UpdateAsync(created.Id, updateRequest);

        // Assert
        Assert.Equal("Sophia Johnson", updated.FullName);
        Assert.Equal(2, updated.CurrentLevelId);
        Assert.Equal("Early Reader & Counter", updated.LevelTitle);
        Assert.Equal("es", updated.PreferredLanguage);
    }

    [Fact]
    public async Task DeleteAsync_ExistingProfile_RemovesFromDatabase()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var service = new ChildProfileService(db);
        var created = await service.CreateAsync(new CreateChildProfileRequest(
            "Noah", null, new DateOnly(2019, 7, 20), 1, "en", null, "noah@test.com"));

        // Act
        var deleted = await service.DeleteAsync(created.Id);

        // Assert
        Assert.True(deleted);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(created.Id));
    }
}
