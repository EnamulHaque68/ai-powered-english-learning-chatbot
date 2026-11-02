using Chatbot.API.Data;
using Chatbot.API.DTOs.Conversation;
using Chatbot.API.Entities;
using Chatbot.API.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.API.Services;

public interface IConversationService
{
    Task<ConversationSessionResponse> StartSessionAsync(StartSessionRequest request);
    Task<IReadOnlyList<ConversationSessionResponse>> GetChildSessionsAsync(Guid childId, int limit = 20);
    Task<SessionDetailResponse> GetSessionDetailAsync(Guid sessionId);
    Task<ConversationMessageResponse> AddMessageAsync(Guid sessionId, AddMessageRequest request);
    Task<ConversationSessionResponse> EndSessionAsync(Guid sessionId, EndSessionRequest request);
}

public class ConversationService : IConversationService
{
    private readonly ChildLearningDbContext _db;

    public ConversationService(ChildLearningDbContext db)
    {
        _db = db;
    }

    public async Task<ConversationSessionResponse> StartSessionAsync(StartSessionRequest request)
    {
        var childExists = await _db.ChildProfiles.AnyAsync(c => c.Id == request.ChildProfileId);
        if (!childExists)
            throw new NotFoundException(nameof(ChildProfile), request.ChildProfileId);

        var title = string.IsNullOrWhiteSpace(request.SessionTitle)
            ? $"Learning Session - {DateTimeOffset.UtcNow:MMM dd, yyyy HH:mm}"
            : request.SessionTitle.Trim();

        var session = new ConversationSession
        {
            Id = Guid.NewGuid(),
            ChildProfileId = request.ChildProfileId,
            SessionTitle = title,
            StartedAt = DateTimeOffset.UtcNow,
            ChildMood = request.InitialMood?.Trim(),
            TotalMessages = 0,
            IsActive = true,
            ActiveMode = request.InitialMode ?? ChatbotMode.Learning
        };

        _db.ConversationSessions.Add(session);
        await _db.SaveChangesAsync();

        return ToSessionResponse(session);
    }

    public async Task<IReadOnlyList<ConversationSessionResponse>> GetChildSessionsAsync(Guid childId, int limit = 20)
    {
        var childExists = await _db.ChildProfiles.AnyAsync(c => c.Id == childId);
        if (!childExists)
            throw new NotFoundException(nameof(ChildProfile), childId);

        limit = Math.Clamp(limit, 1, 100);

        var sessions = await _db.ConversationSessions
            .Where(s => s.ChildProfileId == childId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        return sessions.Select(ToSessionResponse).ToList();
    }

    public async Task<SessionDetailResponse> GetSessionDetailAsync(Guid sessionId)
    {
        var session = await _db.ConversationSessions
            .Include(s => s.ChildProfile)
            .Include(s => s.Messages.OrderBy(m => m.Timestamp))
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            throw new NotFoundException(nameof(ConversationSession), sessionId);

        var child = session.ChildProfile;
        var childName = child != null
            ? (string.IsNullOrWhiteSpace(child.LastName) ? child.FirstName : $"{child.FirstName} {child.LastName}")
            : "Unknown";

        var messages = session.Messages.Select(ToMessageResponse).ToList();

        return new SessionDetailResponse(
            session.Id,
            session.ChildProfileId,
            childName,
            session.SessionTitle,
            session.StartedAt,
            session.EndedAt,
            session.TotalMessages,
            session.ChildMood,
            session.Summary,
            session.IsActive,
            session.ActiveMode,
            session.CurrentTopic,
            session.ConsecutiveCorrectCount,
            session.StarsEarned,
            messages
        );
    }

    public async Task<ConversationMessageResponse> AddMessageAsync(Guid sessionId, AddMessageRequest request)
    {
        var session = await _db.ConversationSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null)
            throw new NotFoundException(nameof(ConversationSession), sessionId);

        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationSessionId = sessionId,
            SenderRole = request.SenderRole,
            MessageText = request.MessageText.Trim(),
            Timestamp = DateTimeOffset.UtcNow,
            MediaUrl = request.MediaUrl?.Trim(),
            TokensCount = request.TokensCount,
            ResponseTimeMs = request.ResponseTimeMs,
            SentimentScore = request.SentimentScore
        };

        _db.ConversationMessages.Add(message);

        session.TotalMessages++;
        await _db.SaveChangesAsync();

        return ToMessageResponse(message);
    }

    public async Task<ConversationSessionResponse> EndSessionAsync(Guid sessionId, EndSessionRequest request)
    {
        var session = await _db.ConversationSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null)
            throw new NotFoundException(nameof(ConversationSession), sessionId);

        session.EndedAt = DateTimeOffset.UtcNow;
        session.IsActive = false;

        if (!string.IsNullOrWhiteSpace(request.ChildMood))
            session.ChildMood = request.ChildMood.Trim();

        if (!string.IsNullOrWhiteSpace(request.Summary))
            session.Summary = request.Summary.Trim();

        await _db.SaveChangesAsync();

        return ToSessionResponse(session);
    }

    private static ConversationSessionResponse ToSessionResponse(ConversationSession s) =>
        new(
            s.Id,
            s.ChildProfileId,
            s.SessionTitle,
            s.StartedAt,
            s.EndedAt,
            s.TotalMessages,
            s.ChildMood,
            s.Summary,
            s.IsActive,
            s.ActiveMode,
            s.CurrentTopic,
            s.ConsecutiveCorrectCount,
            s.StarsEarned
        );

    private static ConversationMessageResponse ToMessageResponse(ConversationMessage m) =>
        new(
            m.Id,
            m.ConversationSessionId,
            m.SenderRole,
            m.SenderRole.ToString(),
            m.MessageText,
            m.Timestamp,
            m.MediaUrl,
            m.TokensCount,
            m.ResponseTimeMs,
            m.SentimentScore
        );
}
