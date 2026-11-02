using System.Net;
using System.Net.Http.Json;
using Chatbot.API.Data;
using Chatbot.API.DTOs.ChildProfile;
using Chatbot.API.DTOs.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Chatbot.API.Tests.IntegrationTests;

public class ChildProfilesApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ChildProfilesApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "IntegrationTestDb_" + Guid.NewGuid();

        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all DbContext-related registrations
                var dbDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ChildLearningDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ChildLearningDbContext)).ToList();

                foreach (var descriptor in dbDescriptors)
                {
                    services.Remove(descriptor);
                }

                // Add InMemory DbContext with dedicated internal service provider
                var inMemoryProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<ChildLearningDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.UseInternalServiceProvider(inMemoryProvider);
                });
            });
        });

        _client = customFactory.CreateClient();

        // Seed initial level data
        using var scope = customFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChildLearningDbContext>();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Post_ValidChildProfile_Returns201Created()
    {
        // Arrange
        var request = new CreateChildProfileRequest(
            FirstName: "Lucas",
            LastName: "Vance",
            DateOfBirth: new DateOnly(2019, 6, 1),
            CurrentLevelId: 1,
            PreferredLanguage: "en",
            Interests: "Robots, Space",
            ParentEmail: "lucas.parent@example.com"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/childprofiles", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ChildProfileResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Equal("Lucas", body.Data?.FirstName);
    }

    [Fact]
    public async Task Post_InvalidChildProfile_Returns400BadRequestWithProblemDetails()
    {
        // Arrange - Invalid: empty first name, future birthdate, invalid email
        var invalidRequest = new CreateChildProfileRequest(
            FirstName: "",
            LastName: null,
            DateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            CurrentLevelId: 0,
            PreferredLanguage: "en",
            Interests: null,
            ParentEmail: "invalid-email"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/childprofiles", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.NotNull(body.Errors);
        Assert.True(body.Errors.Count > 0);
    }

    [Fact]
    public async Task Get_NonExistentChildProfile_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/childprofiles/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Contains("not found", body.Message, StringComparison.OrdinalIgnoreCase);
    }
}
