import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of } from 'rxjs';
import {
  ApiResponse,
  ChatbotMessageRequest,
  ChatbotMode,
  ChatbotReplyResponse,
  ChildLearningLevel,
  ChildProfile,
  ChildProgressSummary,
  CreateChildProfileRequest,
  PagedResult,
  SessionContextDto
} from '../models/chatbot.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ChatbotApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  public getChildren(): Observable<ChildProfile[]> {
    return this.http.get<ApiResponse<PagedResult<ChildProfile>>>(`${this.baseUrl}/childprofiles?page=1&pageSize=20`).pipe(
      map(res => res.data?.items ?? []),
      catchError(err => {
        console.warn('API getChildren failed, using local profile fallback', err);
        return of([this.createDefaultMockProfile()]);
      })
    );
  }

  public createChild(request: CreateChildProfileRequest): Observable<ChildProfile> {
    return this.http.post<ApiResponse<ChildProfile>>(`${this.baseUrl}/childprofiles`, request).pipe(
      map(res => res.data),
      catchError(err => {
        console.warn('API createChild failed, creating local profile fallback', err);
        const fallback: ChildProfile = {
          id: 'local-' + Date.now(),
          firstName: request.firstName,
          lastName: request.lastName,
          fullName: request.lastName ? `${request.firstName} ${request.lastName}` : request.firstName,
          dateOfBirth: request.dateOfBirth,
          age: 4,
          currentLevelId: request.currentLevelId,
          levelTitle: request.currentLevelId === 1 ? 'Toddler Explorer' : request.currentLevelId === 2 ? 'Early Learner' : 'Kinder Explorer',
          preferredLanguage: request.preferredLanguage ?? 'en',
          interests: request.interests,
          parentEmail: request.parentEmail
        };
        return of(fallback);
      })
    );
  }

  public sendMessage(request: ChatbotMessageRequest): Observable<ChatbotReplyResponse> {
    return this.http.post<ApiResponse<ChatbotReplyResponse>>(`${this.baseUrl}/chatbot/chat`, request).pipe(
      map(res => res.data),
      catchError(err => {
        console.warn('API sendMessage offline fallback', err);
        return of(this.generateOfflineReply(request));
      })
    );
  }

  public switchMode(sessionId: string, newMode: ChatbotMode): Observable<string> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/chatbot/switch-mode`, { sessionId, newMode }).pipe(
      map(res => res.data),
      catchError(() => of(ChatbotMode[newMode]))
    );
  }

  public getSessionContext(sessionId: string): Observable<SessionContextDto | null> {
    return this.http.get<ApiResponse<SessionContextDto>>(`${this.baseUrl}/chatbot/sessions/${sessionId}/context`).pipe(
      map(res => res.data),
      catchError(() => of(null))
    );
  }

  public getChildProgress(childId: string): Observable<ChildProgressSummary | null> {
    return this.http.get<ApiResponse<ChildProgressSummary>>(`${this.baseUrl}/children/${childId}/progress/summary`).pipe(
      map(res => res.data),
      catchError(() => of(this.getMockProgress(childId)))
    );
  }

  private createDefaultMockProfile(): ChildProfile {
    return {
      id: 'mock-child-1',
      firstName: 'Leo',
      lastName: null,
      fullName: 'Leo',
      dateOfBirth: '2022-05-15',
      age: 4,
      currentLevelId: 1,
      levelTitle: 'Toddler Explorer',
      preferredLanguage: 'en',
      interests: 'Dinosaurs, Space, Animals',
      parentEmail: 'parent@example.com',
      avatarEmoji: '🦁'
    };
  }

  private getMockProgress(childId: string): ChildProgressSummary {
    return {
      childProfileId: childId,
      childName: 'Leo',
      totalTopicsTracked: 4,
      averageProficiency: 78.5,
      totalActivitiesCompleted: 14,
      totalTimeSpentMinutes: 48,
      topics: [
        {
          id: 'p1',
          childProfileId: childId,
          topic: 'Colors',
          proficiencyScore: 85,
          completedActivitiesCount: 5,
          totalTimeSpentMinutes: 18,
          masteryStatus: 3,
          masteryLabel: 'Proficient',
          lastActivityAt: new Date().toISOString()
        },
        {
          id: 'p2',
          childProfileId: childId,
          topic: 'Animals',
          proficiencyScore: 92,
          completedActivitiesCount: 6,
          totalTimeSpentMinutes: 20,
          masteryStatus: 4,
          masteryLabel: 'Mastered',
          lastActivityAt: new Date().toISOString()
        },
        {
          id: 'p3',
          childProfileId: childId,
          topic: 'Phonics',
          proficiencyScore: 65,
          completedActivitiesCount: 3,
          totalTimeSpentMinutes: 10,
          masteryStatus: 2,
          masteryLabel: 'Developing',
          lastActivityAt: new Date().toISOString()
        }
      ]
    };
  }

  private generateOfflineReply(request: ChatbotMessageRequest): ChatbotReplyResponse {
    const clean = request.message.toLowerCase();
    const mode = request.modeOverride ?? ChatbotMode.Learning;
    const level = request.levelOverride ?? ChildLearningLevel.L1_Toddler;

    let reply = "⭐ YAY! Super job! That is right! 🎉\n\nWhat does a cow say? 🐮 (Moo or Meow)";
    let stars = 1;
    let suggested = ["Moo 🐮", "Meow 🐱", "Woof 🐶"];

    if (mode === ChatbotMode.Engagement) {
      reply = "Riddle time! 🥕 I have long ears, love crunchy carrots, and hop around! Who am I? 🐰";
      suggested = ["A bunny! 🐰", "A lion! 🦁", "A bear! 🐻"];
      stars = 0;
    } else if (mode === ChatbotMode.Support) {
      reply = "Big teddy bear hug! 🧸 You are doing so amazing! Would you like to hear a funny joke? 😊";
      suggested = ["Tell me a joke! 😄", "Tell me a story! 📖", "I feel happy! 💖"];
      stars = 0;
    } else if (mode === ChatbotMode.Conversation) {
      reply = "Hello little superstar! ⭐ I am so happy to chat with you! What is your favorite animal? 🐶🐱";
      suggested = ["I love dogs! 🐶", "I like cats! 🐱", "Dinosaurs! 🦖"];
      stars = 0;
    }

    return {
      sessionId: request.sessionId ?? 'session-' + Date.now(),
      messageId: 'msg-' + Date.now(),
      replyText: reply,
      activeMode: mode,
      activeModeName: ChatbotMode[mode],
      level: level,
      levelName: ChildLearningLevel[level],
      starsAwarded: stars,
      totalSessionStars: 5,
      suggestedReplies: suggested,
      levelProgressionAlert: null,
      responseTimeMs: 8,
      contextTopic: 'Colors',
      sentimentScore: 0.95
    };
  }
}
