namespace Chatbot.API.Entities;

public enum SenderRole
{
    Child = 1,
    AI = 2,
    Parent = 3,
    System = 4
}

public enum MasteryStatus
{
    Novice = 1,
    Developing = 2,
    Proficient = 3,
    Mastered = 4
}

public enum ChatbotMode
{
    Learning = 1,       // Micro-lessons, vocabulary, phonics, question & answer
    Conversation = 2,   // Age-tailored casual companion dialogue
    Engagement = 3,     // Games, guessing riddles, rhymes, songs
    Support = 4         // Emotional comfort, reassurance, help when stuck
}

public enum ChildLearningLevel
{
    L1_Toddler = 1,        // Ages 3-4: 1-2 words, phonics sounds, colors, animal sounds
    L2_EarlyLearner = 2,   // Ages 4-5: 2-4 words, phonics blending, simple choices
    L3_KinderExplorer = 3  // Ages 5-6: 5-8 words, full simple sentences, story prompts
}
