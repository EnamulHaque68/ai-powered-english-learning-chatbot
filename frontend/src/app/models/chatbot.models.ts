export enum ChatbotMode {
  Learning = 1,
  Conversation = 2,
  Engagement = 3,
  Support = 4
}

export enum ChildLearningLevel {
  L1_Toddler = 1,
  L2_EarlyLearner = 2,
  L3_KinderExplorer = 3
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
  timestamp: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ChildProfile {
  id: string;
  firstName: string;
  lastName?: string | null;
  fullName: string;
  dateOfBirth: string;
  age: number;
  currentLevelId: number;
  levelTitle: string;
  preferredLanguage: string;
  interests?: string | null;
  parentEmail: string;
  avatarEmoji?: string;
}

export interface CreateChildProfileRequest {
  firstName: string;
  lastName?: string | null;
  dateOfBirth: string;
  currentLevelId: number;
  preferredLanguage?: string;
  interests?: string | null;
  parentEmail: string;
}

export interface ChatbotMessageRequest {
  childId: string;
  sessionId?: string | null;
  message: string;
  modeOverride?: number | null;
  levelOverride?: number | null;
}

export interface ChatbotReplyResponse {
  sessionId: string;
  messageId: string;
  replyText: string;
  activeMode: number;
  activeModeName: string;
  level: number;
  levelName: string;
  starsAwarded: number;
  totalSessionStars: number;
  suggestedReplies: string[];
  levelProgressionAlert?: string | null;
  responseTimeMs: number;
  contextTopic?: string | null;
  sentimentScore: number;
}

export interface LearningProgressItem {
  id: string;
  childProfileId: string;
  topic: string;
  proficiencyScore: number;
  completedActivitiesCount: number;
  totalTimeSpentMinutes: number;
  masteryStatus: number;
  masteryLabel: string;
  lastActivityAt: string;
  notes?: string | null;
}

export interface ChildProgressSummary {
  childProfileId: string;
  childName: string;
  totalTopicsTracked: number;
  averageProficiency: number;
  totalActivitiesCompleted: number;
  totalTimeSpentMinutes: number;
  topics: LearningProgressItem[];
}

export interface ChatMessage {
  id: string;
  sender: 'child' | 'bot';
  text: string;
  timestamp: Date;
  responseTimeMs?: number;
  starsAwarded?: number;
  mode?: number;
  level?: number;
  isSpeaking?: boolean;
}

export interface AvatarOption {
  id: string;
  name: string;
  emoji: string;
  bgGradient: string;
}

export interface RecentTurnDto {
  userMessage: string;
  botReply: string;
  timestamp: string;
  pendingQuestionAnswer?: string | null;
}

export interface SessionContextDto {
  sessionId: string;
  childId: string;
  childName: string;
  activeMode: number;
  level: number;
  currentTopic: string;
  consecutiveCorrectCount: number;
  starsEarned: number;
  recentTurns: RecentTurnDto[];
}
