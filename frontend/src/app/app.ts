import { Component, OnInit, inject, signal, viewChild, ElementRef, afterNextRender } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AvatarOption,
  ChatMessage,
  ChatbotMode,
  ChildLearningLevel,
  ChildProfile,
  ChildProgressSummary
} from './models/chatbot.models';
import { ChatbotApiService } from './services/chatbot-api.service';
import { SpeechAudioService } from './services/speech-audio.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly api = inject(ChatbotApiService);
  protected readonly audio = inject(SpeechAudioService);

  // Chat scroll container reference
  private readonly messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messagesContainer');

  // Enums for template access
  protected readonly ChatbotMode = ChatbotMode;
  protected readonly ChildLearningLevel = ChildLearningLevel;

  // Avatars for young children
  protected readonly avatars: AvatarOption[] = [
    { id: 'lion', name: 'Leo Lion', emoji: '🦁', bgGradient: 'linear-gradient(135deg, #fef08a, #f59e0b)' },
    { id: 'panda', name: 'Pippa Panda', emoji: '🐼', bgGradient: 'linear-gradient(135deg, #e2e8f0, #94a3b8)' },
    { id: 'monkey', name: 'Mia Monkey', emoji: '🐵', bgGradient: 'linear-gradient(135deg, #fed7aa, #ea580c)' },
    { id: 'bunny', name: 'Bella Bunny', emoji: '🐰', bgGradient: 'linear-gradient(135deg, #fbcfe8, #ec4899)' },
    { id: 'tiger', name: 'Toby Tiger', emoji: '🐯', bgGradient: 'linear-gradient(135deg, #ffedd5, #f97316)' },
    { id: 'unicorn', name: 'Sparkle Unicorn', emoji: '🦄', bgGradient: 'linear-gradient(135deg, #ede9fe, #a855f7)' }
  ];

  // Active state signals
  public activeChild = signal<ChildProfile | null>(null);
  public selectedAvatar = signal<AvatarOption>(this.avatars[0]);
  public activeSessionId = signal<string | null>(null);
  public activeMode = signal<ChatbotMode>(ChatbotMode.Learning);
  public activeLevel = signal<ChildLearningLevel>(ChildLearningLevel.L1_Toddler);
  public totalStars = signal<number>(0);
  public streakCount = signal<number>(0);
  public isLoading = signal<boolean>(false);
  public isListening = signal<boolean>(false);

  // Messages and Quick Replies
  public messages = signal<ChatMessage[]>([]);
  public suggestedReplies = signal<string[]>(['Red 🍓', 'Yellow ☀️', 'Blue 🌊', 'Green 🌿']);
  public inputText = signal<string>('');

  // Modals & Popups
  public showWelcomeModal = signal<boolean>(true);
  public showProgressModal = signal<boolean>(false);
  public levelUpAlert = signal<string | null>(null);
  public starCelebration = signal<boolean>(false);
  public progressSummary = signal<ChildProgressSummary | null>(null);

  // Welcome Form Inputs
  public newChildName = signal<string>('Leo');
  public newChildAge = signal<number>(4);
  public newChildInterest = signal<string>('Animals & Dinosaurs');

  constructor() {
    afterNextRender(() => {
      this.scrollToBottom();
    });
  }

  public ngOnInit(): void {
    // Attempt to load existing profiles from API
    this.api.getChildren().subscribe(children => {
      if (children.length > 0) {
        this.selectChild(children[0]);
        this.showWelcomeModal.set(false);
      }
    });
  }

  public selectAvatar(avatar: AvatarOption): void {
    this.selectedAvatar.set(avatar);
    this.audio.playStarSound();
  }

  public submitWelcomeForm(): void {
    const name = this.newChildName().trim() || 'Superstar';
    const age = this.newChildAge();
    const levelId = age <= 4 ? 1 : age === 5 ? 2 : 3;

    const request = {
      firstName: name,
      lastName: null,
      dateOfBirth: this.calculateDobFromAge(age),
      currentLevelId: levelId,
      preferredLanguage: 'en',
      interests: this.newChildInterest(),
      parentEmail: 'parent@example.com'
    };

    this.isLoading.set(true);
    this.api.createChild(request).subscribe(profile => {
      this.isLoading.set(false);
      this.selectChild(profile);
      this.showWelcomeModal.set(false);
      this.audio.playLevelUpFanfare();
    });
  }

  public selectChild(profile: ChildProfile): void {
    this.activeChild.set(profile);
    const resolvedLevel = (profile.currentLevelId === 1)
      ? ChildLearningLevel.L1_Toddler
      : (profile.currentLevelId === 2)
      ? ChildLearningLevel.L2_EarlyLearner
      : ChildLearningLevel.L3_KinderExplorer;

    this.activeLevel.set(resolvedLevel);

    // Initial greeting message from Buddy
    const greetingText = resolvedLevel === ChildLearningLevel.L1_Toddler
      ? `Hello ${profile.firstName}! ⭐ I'm Buddy! Let's play with colors! 🎨 What color is a red strawberry? 🍓 (Red or Blue)`
      : resolvedLevel === ChildLearningLevel.L2_EarlyLearner
      ? `Welcome ${profile.firstName}! 🌟 I'm Buddy your learning pal! What color is a banana: Yellow or Purple? 🍌`
      : `Hello explorer ${profile.firstName}! 🚀 Welcome to Word Adventure! What word rhymes with CAT: Hat or Dog? 🎩`;

    this.messages.set([
      {
        id: 'msg-init',
        sender: 'bot',
        text: greetingText,
        timestamp: new Date(),
        responseTimeMs: 5,
        mode: ChatbotMode.Learning,
        level: resolvedLevel
      }
    ]);

    // Speak initial welcome aloud if enabled
    this.audio.speak(greetingText);
    this.refreshProgress();
  }

  public setMode(mode: ChatbotMode): void {
    this.activeMode.set(mode);
    const sessionId = this.activeSessionId();
    if (sessionId) {
      this.api.switchMode(sessionId, mode).subscribe();
    }

    // Update suggested replies based on mode & level
    this.updateSuggestedForMode(mode);

    // Send a mode greeting transition
    const promptMessage = mode === ChatbotMode.Learning
      ? 'Let’s do a fun learning quiz! 🎓'
      : mode === ChatbotMode.Engagement
      ? 'Let’s play an animal game riddle! 🎈'
      : mode === ChatbotMode.Support
      ? 'I need some comfort or help 🧸'
      : 'Let’s just chat and be friends! 💬';

    this.sendMessageWithText(promptMessage, mode);
  }

  public setLevel(level: ChildLearningLevel): void {
    this.activeLevel.set(level);
    this.audio.playStarSound();

    const child = this.activeChild();
    if (child) {
      child.currentLevelId = level;
      child.levelTitle = level === 1 ? 'Toddler Explorer' : level === 2 ? 'Early Learner' : 'Kinder Explorer';
    }

    const levelAnnounce = level === ChildLearningLevel.L1_Toddler
      ? "We switched to Level 1: Toddler Explorer! 🎨 Colors and happy animal sounds!"
      : level === ChildLearningLevel.L2_EarlyLearner
      ? "We switched to Level 2: Early Learner! 🌟 Fun words and phonics blending!"
      : "We switched to Level 3: Kinder Explorer! 🚀 Stories, sentences, and clever rhymes!";

    this.messages.update(prev => [
      ...prev,
      {
        id: 'lvl-' + Date.now(),
        sender: 'bot',
        text: levelAnnounce,
        timestamp: new Date(),
        level: level,
        mode: this.activeMode()
      }
    ]);

    this.audio.speak(levelAnnounce);
  }

  public onSendClick(): void {
    const text = this.inputText().trim();
    if (!text || this.isLoading()) return;
    this.sendMessageWithText(text);
    this.inputText.set('');
  }

  public onQuickReplyClick(reply: string): void {
    if (this.isLoading()) return;
    this.sendMessageWithText(reply);
  }

  public speakMessage(msg: ChatMessage): void {
    msg.isSpeaking = true;
    this.audio.speak(msg.text, () => {
      msg.isSpeaking = false;
    });
  }

  public toggleVoice(): void {
    this.audio.toggleSpeech();
  }

  public toggleProgressModal(): void {
    const next = !this.showProgressModal();
    this.showProgressModal.set(next);
    if (next) {
      this.refreshProgress();
    }
  }

  public closeLevelUpAlert(): void {
    this.levelUpAlert.set(null);
  }

  public restartAdventure(): void {
    this.showWelcomeModal.set(true);
  }

  public closeWelcomeModal(): void {
    this.showWelcomeModal.set(false);
    if (!this.activeChild()) {
      const name = this.newChildName().trim() || 'Explorer';
      const age = this.newChildAge();
      const levelId = age <= 4 ? 1 : age === 5 ? 2 : 3;
      const levelTitle = levelId === 1 ? 'Toddler Explorer' : levelId === 2 ? 'Early Learner' : 'Kinder Explorer';
      const fallbackChild: ChildProfile = {
        id: 'child-default',
        firstName: name,
        lastName: null,
        fullName: name,
        dateOfBirth: this.calculateDobFromAge(age),
        age: age,
        currentLevelId: levelId,
        levelTitle: levelTitle,
        preferredLanguage: 'en',
        interests: this.newChildInterest(),
        parentEmail: 'parent@example.com',
        avatarEmoji: this.selectedAvatar().emoji
      };
      this.selectChild(fallbackChild);
    }
  }

  private sendMessageWithText(text: string, forceMode?: ChatbotMode): void {
    const child = this.activeChild();
    if (!child) return;

    // 1. Add child message locally
    const childMsg: ChatMessage = {
      id: 'child-' + Date.now(),
      sender: 'child',
      text: text,
      timestamp: new Date()
    };

    this.messages.update(prev => [...prev, childMsg]);
    this.scrollToBottom();

    // 2. Request from Chatbot API
    this.isLoading.set(true);

    const payload = {
      childId: child.id,
      sessionId: this.activeSessionId(),
      message: text,
      modeOverride: forceMode ?? this.activeMode(),
      levelOverride: this.activeLevel()
    };

    this.api.sendMessage(payload).subscribe(res => {
      this.isLoading.set(false);
      this.activeSessionId.set(res.sessionId);
      this.activeMode.set(res.activeMode);
      this.activeLevel.set(res.level);
      this.totalStars.set(res.totalSessionStars);
      this.suggestedReplies.set(res.suggestedReplies ?? []);

      // Bot message
      const botMsg: ChatMessage = {
        id: res.messageId,
        sender: 'bot',
        text: res.replyText,
        timestamp: new Date(),
        responseTimeMs: res.responseTimeMs,
        starsAwarded: res.starsAwarded,
        mode: res.activeMode,
        level: res.level
      };

      this.messages.update(prev => [...prev, botMsg]);
      this.scrollToBottom();

      // Trigger Voice Read-Aloud
      this.audio.speak(res.replyText);

      // Star celebration sound and animation
      if (res.starsAwarded > 0) {
        this.starCelebration.set(true);
        this.audio.playStarSound();
        setTimeout(() => this.starCelebration.set(false), 2200);
      }

      // Level-up celebration
      if (res.levelProgressionAlert) {
        this.levelUpAlert.set(res.levelProgressionAlert);
        this.audio.playLevelUpFanfare();
      }

      this.refreshProgress();
    });
  }

  private refreshProgress(): void {
    const child = this.activeChild();
    if (!child) return;
    this.api.getChildProgress(child.id).subscribe(prog => {
      if (prog) {
        this.progressSummary.set(prog);
      }
    });
  }

  private updateSuggestedForMode(mode: ChatbotMode): void {
    const level = this.activeLevel();
    if (mode === ChatbotMode.Engagement) {
      this.suggestedReplies.set(['A bunny! 🐰', 'A lion! 🦁', 'Twinkle Star! ✨']);
    } else if (mode === ChatbotMode.Support) {
      this.suggestedReplies.set(['Tell me a joke! 😄', 'Tell me a story! 📖', 'I need a hug! 🧸']);
    } else if (mode === ChatbotMode.Conversation) {
      this.suggestedReplies.set(['I like puppies! 🐶', 'I love dinosaurs! 🦖', 'My favorite is blue! 💙']);
    } else {
      if (level === ChildLearningLevel.L1_Toddler) {
        this.suggestedReplies.set(['Red 🍓', 'Moo 🐮', 'Yellow ☀️', 'A for Apple 🍎']);
      } else if (level === ChildLearningLevel.L2_EarlyLearner) {
        this.suggestedReplies.set(['Yellow 🍌', 'Cat 🐱', 'Bird 🐦', 'Sky ☀️']);
      } else {
        this.suggestedReplies.set(['Hat 🎩', 'Cold ❄️', 'Trees 🌳', 'Dolphin 🐬']);
      }
    }
  }

  private calculateDobFromAge(age: number): string {
    const currentYear = new Date().getFullYear();
    const birthYear = currentYear - age;
    return `${birthYear}-05-15`;
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const el = this.messagesContainer()?.nativeElement;
      if (el) {
        el.scrollTop = el.scrollHeight;
      }
    }, 80);
  }
}
