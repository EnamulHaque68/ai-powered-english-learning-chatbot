using Chatbot.API.Data;
using Chatbot.API.Middleware;
using Chatbot.API.Services;
using Chatbot.API.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure EF Core with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.\\SQLEXPRESS;Database=ChildLearningChatbotDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<ChildLearningDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
});

// 2. Register Application Services
builder.Services.AddScoped<IChildProfileService, ChildProfileService>();
builder.Services.AddScoped<ILearningProgressService, LearningProgressService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<ILearningLevelService, LearningLevelService>();

// Chatbot Engine & Safety Services
builder.Services.AddScoped<Chatbot.API.Services.ChildSafety.IChildSafetyService, Chatbot.API.Services.ChildSafety.ChildSafetyService>();
builder.Services.AddScoped<Chatbot.API.Services.Chatbot.IChatbotContextService, Chatbot.API.Services.Chatbot.ChatbotContextService>();
builder.Services.AddScoped<Chatbot.API.Services.Chatbot.ILevelProgressionService, Chatbot.API.Services.Chatbot.LevelProgressionService>();
builder.Services.AddScoped<Chatbot.API.Services.Chatbot.IRuleBasedChatbotEngine, Chatbot.API.Services.Chatbot.RuleBasedChatbotEngine>();

// 3. Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateChildProfileValidator>();

// 4. Register Global Exception Handling & ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 5. Configure CORS for Angular Frontend
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200", "https://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins(configuredOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 6. Controllers & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 7. Configure HTTP Request Pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngularDev");
app.UseAuthorization();
app.MapControllers();

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
