using System.Text.RegularExpressions;
using Chatbot.API.Entities;

namespace Chatbot.API.Services.ChildSafety;

public record SafetyResult(
    bool IsSafe,
    string? TriggeredCategory,
    string? SafeResponse,
    bool SuggestSupportMode = false
);

public interface IChildSafetyService
{
    SafetyResult EvaluateInput(string input, ChildLearningLevel level);
}

public class ChildSafetyService : IChildSafetyService
{
    private static readonly HashSet<string> DangerWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "kill", "murder", "blood", "die", "death", "gun", "knife", "weapon", "shoot",
        "bomb", "fight", "punch", "hurt", "stranger", "kidnap", "poison", "monster",
        "nightmare", "scary", "ghost", "hate", "ugly", "stupid", "dumb", "bad", "idiot"
    };

    private static readonly HashSet<string> InappropriateAdultWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "sex", "naked", "nude", "porn", "beer", "wine", "alcohol", "drug", "smoke", "cigarette", "vape"
    };

    private static readonly Regex PiiPattern = new(
        @"\b(\d{3}[-.\s]??\d{3}[-.\s]??\d{4}|\d{10})\b" + // Phone numbers
        @"|\b(my address is|i live at|what is your phone|give me your phone|where do you live|what school do you go to)\b" +
        @"|\b([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})\b", // Email in chat
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly HashSet<string> DistressWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "sad", "crying", "cry", "scared", "afraid", "lonely", "mad", "angry", "tired", "giving up", "too hard", "hate this"
    };

    public SafetyResult EvaluateInput(string input, ChildLearningLevel level)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new SafetyResult(true, null, null);

        var tokens = Regex.Matches(input.ToLowerInvariant(), @"\b[a-z']+\b")
            .Select(m => m.Value)
            .ToHashSet();

        // 1. Check for emotional distress -> warm support redirect
        if (tokens.Any(t => DistressWords.Contains(t)) || input.Contains("i am sad", StringComparison.OrdinalIgnoreCase))
        {
            var comfortReply = level switch
            {
                ChildLearningLevel.L1_Toddler =>
                    "Big teddy bear hug! 🧸 You are safe and loved. Let's smile together! 😊 Would you like a happy animal sound?",
                ChildLearningLevel.L2_EarlyLearner =>
                    "It is okay to feel sad or tired sometimes! 💖 I am right here with you. Take a soft breath: in... and out... 🫧 Would you like a cozy story or a cute puppy sound? 🐶",
                _ =>
                    "Thank you for sharing your feelings with me. You are so brave and wonderful! 🌟 It's completely okay to feel this way. Let's take a deep belly breath together! 🌈 Would you like to hear a gentle story or play an easy game?"
            };

            return new SafetyResult(false, "EmotionalDistress", comfortReply, SuggestSupportMode: true);
        }

        // 2. Check for PII disclosure or requests
        if (PiiPattern.IsMatch(input))
        {
            var piiReply = level switch
            {
                ChildLearningLevel.L1_Toddler =>
                    "Shh, secret superhero time! 🦸 We keep secrets safe. Can you roar like a lion? Rrrroar! 🦁",
                ChildLearningLevel.L2_EarlyLearner =>
                    "We always keep our real names, phone numbers, and addresses secret like secret agents! 🕶️ You can call me Buddy! What is your favorite animal? 🐼",
                _ =>
                    "Safety reminder: We always protect our personal information online! Let's keep your address and phone number secret. 🛡️ Let's get back to our fun English quest! What topic shall we explore next? 🚀"
            };

            return new SafetyResult(false, "PII_Protection", piiReply);
        }

        // 3. Check for danger / scary words
        if (tokens.Any(t => DangerWords.Contains(t)))
        {
            var safeReply = level switch
            {
                ChildLearningLevel.L1_Toddler =>
                    "Look at the fluffy puppy! 🐶 Woof woof! Let's clap our hands! 👏 Can you say puppy?",
                ChildLearningLevel.L2_EarlyLearner =>
                    "Let's focus on happy and fun things! 🌟 Look at the bright yellow sun! ☀️ What is your favorite bright color? 🎨",
                _ =>
                    "That sounds a little scary or unfriendly. Let's switch to something happy and fun! 🎈 Have you ever seen a rainbow butterfly? 🦋 What colors would your butterfly have?"
            };

            return new SafetyResult(false, "DangerOrScary", safeReply);
        }

        // 4. Check for adult/inappropriate content
        if (tokens.Any(t => InappropriateAdultWords.Contains(t)))
        {
            var safeReply = "Let's play with our favorite toys and colors! 🎨 Look at this bright red fire truck! 🚒 Wee-woo! Can you make the siren sound?";
            return new SafetyResult(false, "InappropriateContent", safeReply);
        }

        return new SafetyResult(true, null, null);
    }
}
