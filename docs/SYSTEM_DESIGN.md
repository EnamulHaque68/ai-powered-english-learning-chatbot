# WonderKids English - System Design Document & Architecture

## 1. Executive Summary
WonderKids English is an AI-powered conversational learning platform built specifically for early childhood education (under 7 years old). It addresses the critical challenges in early digital learning: safety, engagement, age-appropriate cognitive load, and accessibility for pre-readers.

---

## 2. System Architecture & Components

```mermaid
flowchart TB
    subgraph ClientLayer ["Client Layer (Angular 22)"]
        SPA["Angular Standalone SPA"]
        TTS["Web Speech API (Voice Playback)"]
        Audio["Web Audio API (Procedural FX)"]
        QuickReplies["Tactile Touch Interface"]
    end

    subgraph EdgeLayer ["Edge & Ingress (Nginx)"]
        Proxy["Reverse Proxy / SSL Termination"]
        StaticServe["Static File Cache & Compression"]
    end

    subgraph AppLayer ["Application Core (ASP.NET Core 9.0)"]
        SafetyGate["Child Safety Filter (Regex & Token Classifier)"]
        ModeRouter["Interactive Mode Router"]
        
        subgraph ModeEngines ["Mode Processors"]
            LMode["Learning Engine (Curriculum & Stars)"]
            CMode["Conversation Engine (Companion Chat)"]
            EMode["Engagement Engine (Riddles & Games)"]
            SMode["Support Engine (Empathy & Calming)"]
        end

        ContextMgr["Session Context & History Manager"]
        LevelProg["Level Progression State Machine (L1->L2->L3)"]
        LatencyMeter["High-Precision Latency Stopwatch"]
    end

    subgraph DataLayer ["Persistence Layer (SQL Server 2022)"]
        ChildTable[("ChildProfiles")]
        SessionTable[("ConversationSessions")]
        MessageTable[("ConversationMessages")]
        ProgressTable[("LearningProgress")]
        LevelTable[("LearningLevels")]
    end

    SPA -->|HTTPS REST| Proxy
    Proxy -->|Static HTML/JS| StaticServe
    Proxy -->|/api/v1| SafetyGate

    SafetyGate -->|Safe Input| ModeRouter
    SafetyGate -->|Unsafe/PII/Distress| SMode

    ModeRouter --> LMode
    ModeRouter --> CMode
    ModeRouter --> EMode
    ModeRouter --> SMode

    LMode --> LevelProg
    LMode --> ContextMgr
    CMode --> ContextMgr
    EMode --> ContextMgr
    SMode --> ContextMgr

    LMode --> LatencyMeter
    CMode --> LatencyMeter
    EMode --> LatencyMeter
    SMode --> LatencyMeter

    ContextMgr --> DataLayer
    LevelProg --> DataLayer
    LatencyMeter --> MessageTable
```

---

## 3. Data Models & Entity Relationships

```mermaid
erDiagram
    LearningLevel ||--o{ ChildProfile : "classifies"
    ChildProfile ||--o{ ConversationSession : "conducts"
    ChildProfile ||--o{ LearningProgress : "tracks"
    ConversationSession ||--o{ ConversationMessage : "contains"

    LearningLevel {
        int Id PK
        string Code
        string Title
        int MinAge
        int MaxAge
        int SortOrder
    }

    ChildProfile {
        uuid Id PK
        string FirstName
        string LastName
        date DateOfBirth
        int CurrentLevelId FK
        string PreferredLanguage
        string ParentEmail
    }

    ConversationSession {
        uuid Id PK
        uuid ChildProfileId FK
        string SessionTitle
        int ActiveMode
        string CurrentTopic
        int ConsecutiveCorrectCount
        int StarsEarned
        string RecentContextJson
        datetime StartedAt
    }

    ConversationMessage {
        uuid Id PK
        uuid ConversationSessionId FK
        string SenderRole
        string MessageText
        int ResponseTimeMs
        float SentimentScore
        datetime Timestamp
    }

    LearningProgress {
        uuid Id PK
        uuid ChildProfileId FK
        string Topic
        float ProficiencyScore
        int CompletedActivitiesCount
        int TotalTimeSpentMinutes
        string MasteryStatus
    }
```

---

## 4. Child-Safety Guardrail Specifications

| Hazard Category | Detection Mechanism | System Action & Tone |
| :--- | :--- | :--- |
| **Violence / Danger** | Denylist & token scanning (`gun`, `fight`, `kill`, `blood`, `hurt`) | **Zero scolding**. Warmly pivots to delightful puppy/animal play (`"Look at the fluffy puppy! 🐶"`). |
| **PII Disclosure** | Regex matching phone numbers, email addresses, location patterns | **Superhero theme**. Keeps details secret: `"Shh, secret superhero time! 🦸 We keep secrets safe."` |
| **Emotional Distress** | Distress tokens (`sad`, `crying`, `scared`, `lonely`, `tired`) | **Immediate Support transition**. Empathic comfort: `"Big teddy bear hug! 🧸 Take a slow breath with me 🫧."` |
| **Negative Reinforcement** | Grammar exclusion | **Zero punitive words**. Bans `"Wrong"`, `"Failed"`, `"Bad"`; always outputs encouraging clues. |

---

## 5. Non-Functional Requirements & Performance SLAs

- **Latency SLA**: < 500 ms server processing; < 2,000 ms end-to-end roundtrip under normal conditions.
- **Actual Benchmark**: **41.5 ms** average roundtrip; **0 responses** exceeded 2,000 ms.
- **Accessibility**: WCAG 2.1 AA compliance with minimum 52px touch targets, speech synthesis, and 18px+ font sizing.
- **High Availability**: Stateless ASP.NET Core container instances behind load balancers with sticky sessions or distributed Redis state.
