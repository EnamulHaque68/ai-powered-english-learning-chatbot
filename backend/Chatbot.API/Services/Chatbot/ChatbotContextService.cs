using System.Text.Json;
using Chatbot.API.DTOs.Conversation;
using Chatbot.API.Entities;

namespace Chatbot.API.Services.Chatbot;

public class SessionContextState
{
    public ChatbotMode ActiveMode { get; set; } = ChatbotMode.Learning;
    public string CurrentTopic { get; set; } = "Colors";
    public string? CurrentQuestionId { get; set; }
    public string? PromptQuestion { get; set; }
    public string? ExpectedAnswer { get; set; }
    public int ConsecutiveCorrectCount { get; set; } = 0;
    public int TotalStars { get; set; } = 0;
    public List<RecentTurnDto> RecentTurns { get; set; } = new();
}

public interface IChatbotContextService
{
    SessionContextState GetState(ConversationSession session);
    void UpdateState(ConversationSession session, SessionContextState state);
    void AddTurn(SessionContextState state, string userMessage, string botReply, string? pendingAnswer = null);
}

public class ChatbotContextService : IChatbotContextService
{
    private const int MaxHistoryTurns = 5;

    public SessionContextState GetState(ConversationSession session)
    {
        if (string.IsNullOrWhiteSpace(session.RecentContextJson))
        {
            return new SessionContextState
            {
                ActiveMode = session.ActiveMode,
                CurrentTopic = session.CurrentTopic ?? "Colors",
                ConsecutiveCorrectCount = session.ConsecutiveCorrectCount,
                TotalStars = session.StarsEarned
            };
        }

        try
        {
            var state = JsonSerializer.Deserialize<SessionContextState>(session.RecentContextJson);
            if (state != null)
            {
                // Synchronize with database columns
                state.ActiveMode = session.ActiveMode;
                state.ConsecutiveCorrectCount = session.ConsecutiveCorrectCount;
                state.TotalStars = session.StarsEarned;
                return state;
            }
        }
        catch
        {
            // Fallback gracefully on corrupt json
        }

        return new SessionContextState
        {
            ActiveMode = session.ActiveMode,
            CurrentTopic = session.CurrentTopic ?? "Colors",
            ConsecutiveCorrectCount = session.ConsecutiveCorrectCount,
            TotalStars = session.StarsEarned
        };
    }

    public void UpdateState(ConversationSession session, SessionContextState state)
    {
        session.ActiveMode = state.ActiveMode;
        session.CurrentTopic = state.CurrentTopic;
        session.ConsecutiveCorrectCount = state.ConsecutiveCorrectCount;
        session.StarsEarned = state.TotalStars;

        session.RecentContextJson = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    public void AddTurn(SessionContextState state, string userMessage, string botReply, string? pendingAnswer = null)
    {
        state.RecentTurns.Add(new RecentTurnDto(
            UserMessage: userMessage,
            BotReply: botReply,
            Timestamp: DateTimeOffset.UtcNow,
            PendingQuestionAnswer: pendingAnswer
        ));

        if (state.RecentTurns.Count > MaxHistoryTurns)
        {
            state.RecentTurns.RemoveAt(0);
        }
    }
}
