# AI Recommendation Module — Plan

**Project:** AI-Powered Student Skill Exchange and Peer Learning Platform
**Module:** AI Recommendation Module (the "AI Service" actor, Requirement Analysis §4)
**Covers:** recommendation logic and required data flow

---

## 1. Scope

From the Requirement Analysis, this module owns five responsibilities (§5, AI Service):

| # | Requirement | Where it lives |
|---|-------------|----------------|
| 1 | Analyse student-provided skill descriptions | `ISkillAnalysisService.AnalyseAsync` — see §5.4, needs a schema addition owned by another member |
| 2 | Identify related or similar skills | `SkillAnalysisResult.RelatedSkills` |
| 3 | Recommend suitable peer-learning partners | `IRecommendationService.GetRecommendationsAsync` |
| 4 | Generate match scores / explanations | `MentorRecommendationViewModel.MatchScore`, `.Reasons`, `.AiExplanation` |
| 5 | Provide optional learning-path recommendations | `SkillAnalysisResult.LearningPath` |

It sits between **"Add Skills & Learning Goals"** and **"Send Learning Request"** in the system
workflow (§10), and maps to the *AI service* row of the Requirement-to-Development mapping (§11).

**Out of scope for this module:** creating requests, scheduling sessions, feedback capture,
admin dashboard. This module only *reads* those tables to score reputation.

---

## 2. Why two layers, not one

§1 of the Requirement Analysis states the core problem plainly:

> "Traditional searching based only on skill names may also fail to identify related or
> relevant skills."

A single exact-match query on `StudentSkill.SkillId` would reproduce exactly that failure.
So the module is split into two stages:

```
Stage A — AI Skill Analysis     (semantic, LLM)      → what else counts as relevant?
Stage B — AI Peer Recommendation (deterministic)     → who is the best partner, and why?
```

Stage A is where the intelligence lives: it expands one stated learning goal into the wider set
of skills that are actually relevant to it. Stage B is deliberately deterministic — scores
must be reproducible, explainable, and fast, which §6 requires under Performance and Reliability.

---

## 3. Architecture

```
Views/Recommendations/Index.cshtml
        ▲
        │  RecommendationsViewModel
        │
Controllers/RecommendationsController      [Authorize]  ← Acceptance Criteria §9.1
        ▲
        │
Services/IRecommendationService  ──────────►  Services/AI/ISkillAnalysisService
   (RecommendationService)                        (GeminiSkillAnalysisService)
        │                                                   │
        │                                          Services/AI/GeminiClient
        │                                                   │
        │                                          Gemini API (free tier)
        │                                                   │
        │                                          offline fallback analyser
        ▼
Data/ApplicationDbContext (EF Core → SQL Server LocalDB)
```

Every arrow crosses an interface, so the AI provider can be swapped without touching
the ranking logic — this satisfies **Maintainability** (§6).

### File inventory

| File | Role |
|------|------|
| `Services/AI/ISkillAnalysisService.cs` | AI Service contract |
| `Services/AI/GeminiSkillAnalysisService.cs` | LLM analysis + offline fallback |
| `Services/AI/GeminiClient.cs` | REST wrapper over `generateContent` |
| `Services/AI/GeminiOptions.cs` | API key, model, timeout, cache, quota knobs |
| `Services/AI/SkillAnalysisModels.cs` | Analysis request/result types |
| `Services/IRecommendationService.cs` | Peer-ranking contract |
| `Services/RecommendationService.cs` | The six-signal scoring engine |
| `Services/RecommendationOptions.cs` | Tunable weights |
| `Models/ViewModels/RecommendationViewModels.cs` | Presentation types |
| `Controllers/RecommendationsController.cs` | `/Recommendations` + `/Recommendations/Api` |
| `Views/Recommendations/Index.cshtml` | Result page |

---

## 4. Data flow

### 4.1 End-to-end sequence

```
Student opens /Recommendations
   │
   ├─(1) LoadLearnerProfileAsync
   │     StudentSkills WHERE StudentId = me
   │       → goals     (Type = ToLearn)  + skill name, category, level
   │       → offerings (Type = ToTeach)  + skill name, category, level
   │     Guard: no goals → stop, show "add a learning goal" (Acceptance Criteria §9.2)
   │
   ├─(2) RunAnalysisAsync            ── STAGE A: AI SKILL ANALYSIS
   │     Skills catalogue (capped at MaxCatalogSkills)
   │       + the learner's goals and offerings
   │       → Gemini generateContent (JSON mode)
   │       → { relatedSkills[], keywords[], learningPath[] }
   │     Validate every returned skillId against the catalogue
   │     Cache the result for CacheMinutes
   │     On any failure → offline analyser (category + token overlap)
   │
   ├─(3) RankMentorsAsync            ── STAGE B: PEER RECOMMENDATION
   │     searchIds = goalIds ∪ relatedIds(similarity ≥ MinimumRelatedSimilarity)
   │     Candidates: StudentSkills WHERE Type = ToTeach
   │                   AND SkillId IN searchIds
   │                   AND StudentId ≠ me
   │
   ├─(4) Enrichment (one query each, all filtered to the candidate set)
   │     • mentor ToLearn skills   → reciprocity
   │     • LearningRequests        → already-open request to this mentor?
   │     • Feedbacks → Session → Request.ReceiverId → average rating
   │     • LearningSessions (Completed) → sessions taught
   │
   ├─(5) Score each candidate in memory → 0-100
   │
   ├─(6) Rank, cut to MaxResults
   │
   ├─(7) ExplainMatchesAsync → LLM writes a one-line reason for the top N
   │
   └─(8) Render: score + badges + reasons + breakdown + learning path
```

### 4.2 Entities read

| Entity | Fields used | Purpose |
|--------|-------------|---------|
| `StudentSkill` | `StudentId`, `SkillId`, `Type`, `Level` | Goals, offerings, AI input |
| `Skill` | `Id`, `Name`, `Category` | The catalogue the AI may pick from |
| `ApplicationUser` | `Id`, `FullName`, `Bio` | Mentor card |
| `LearningRequest` | `SenderId`, `ReceiverId`, `Status` | Duplicate-request penalty |
| `LearningSession` | `RequestId`, `Status` | Experience signal |
| `Feedback` | `SessionId`, `ReviewerId`, `Rating` | Reputation signal |

**No schema change is made by this module.** It reads the existing entities only, adds no
migration, and does not modify `ApplicationDbContextModelSnapshot.cs` — so it cannot conflict
with migrations written by other members. See §5.4 for the one schema addition this module
would benefit from, raised as a request to the entity owner rather than applied unilaterally.

**Direction convention:** in a `LearningRequest` the **Sender is the learner** and the
**Receiver is the mentor**. Mentor reputation is therefore computed from feedback on sessions
whose `Request.ReceiverId` is the mentor, excluding self-reviews.

---

## 5. Recommendation logic

### 5.1 Stage A — related-skill expansion

The LLM is given the learner's goals and offerings plus the catalogue, and asked to return, for
each goal, catalogue skills that are related (same field, prerequisite, or commonly learned
together) with a `similarity` in 0–1 and a short reason.

Three guardrails, because model output is never trusted directly:

1. Every returned `skillId` must exist in the catalogue — invented ids are dropped.
2. A skill that is already a direct goal cannot also be a related match.
3. Similarity below `MinimumRelatedSimilarity` (0.35) is discarded.

If the reply survives validation with nothing usable in it, the offline analyser runs instead.

### 5.2 Stage B — the six signals

Each signal is normalised to 0–1, then blended by configurable weight:

| # | Signal | Definition | Default weight |
|---|--------|------------|----------------|
| 1 | Skill match | direct matches ÷ goal count | **0.35** |
| 2 | Related skills | Σ similarity of related matches ÷ goal count | **0.15** |
| 3 | Proficiency gap | mean of the level-gap curve below | **0.15** |
| 4 | Reciprocity | 0 / 0.8 / 1.0 for 0 / 1 / 2+ skills they want from you | **0.20** |
| 5 | Rating | average rating ÷ 5, or 0.60 baseline if unrated | **0.10** |
| 6 | Experience | completed sessions ÷ 5, capped at 1 | **0.05** |

Level-gap curve (mentor level minus learner level):

| Gap | Score | Meaning |
|-----|-------|---------|
| ≥ 2 | 1.00 | Expert teaching a beginner |
| 1 | 0.85 | One clear step ahead |
| 0 | 0.50 | Peer practice, still useful |
| < 0 | 0.15 | Below the learner — almost never right |

Final score:

```
score = ( Σ signalᵢ × weightᵢ ) / ( Σ weightᵢ ) × 100
if an open request already exists:  score × 0.85
drop anything below MinimumScoreThreshold (10)
```

Weights are normalised by their own sum, so editing one value in `appsettings.json` cannot
silently break the 0–100 range.

**Reciprocity is weighted at 0.20 — as high as anything but the direct skill match — on
purpose.** The platform is a skill *exchange* (§2), not a tutoring directory. A pair where each
side teaches the other is the outcome the business goal actually wants, so the ranking says so.

### 5.3 Explanations

Acceptance Criteria §9.6 requires a score **or** reason per recommendation. The module gives both,
from two independent sources:

- **Rule-based reasons** — generated deterministically from the score inputs. Always present,
  even with the LLM switched off entirely.
- **LLM one-liner** — a friendly sentence for the top N matches, constrained to the facts passed
  in. Purely additive; if the call fails the card still renders complete.

This layering is deliberate: the explanation shown to a student must never depend on a
third-party API being reachable.

### 5.4 Proposed schema addition (not applied — owned by another member)

Requirement §5.1 asks the AI Service to *"analyse student-provided skill descriptions"*. The
current schema has no free-text field, so there is nothing to analyse: `StudentSkill` carries
only `SkillId`, `Type` and `Level`.

This module therefore analyses **skill name + category + proficiency level**, which already
exist. That is enough to identify related skills, but it cannot pick up a student's own
phrasing of what they want ("I want React specifically for building dashboards").

**Requested change** — one nullable column on each of two entities, owned by the entity author:

```csharp
// Models/Skill.cs
[StringLength(500)]
public string? Description { get; set; }

// Models/StudentSkill.cs
[StringLength(500)]
public string? Description { get; set; }
```

This module is already built to consume it: `AnalysedSkillInput.Description` and
`CatalogSkill.Description` exist and are threaded through the prompt and the offline analyser.
Once the columns land, wiring them up is two assignments in `RecommendationService`
(`ToAnalysisInput` and the catalogue projection) — both marked with a comment in the code.

Deliberately **not** done here: adding the properties and generating the migration would modify
two files this module does not own and rewrite `ApplicationDbContextModelSnapshot.cs`, which
would collide with every other member's migration work.

---

## 6. LLM integration (free tier)

| Setting | Value |
|---------|-------|
| Provider | Google Gemini via AI Studio **free tier** |
| Model | `gemini-2.0-flash` (highest free quota of the Gemini family) |
| Endpoint | `POST {BaseUrl}/models/{Model}:generateContent` |
| Auth | `x-goog-api-key` header |
| Output | `responseMimeType: application/json`, temperature 0.2 |

### Staying inside the free quota

1. **Caching** — analysis results are held in `IMemoryCache` for `CacheMinutes` (60), keyed by
   learner + goals + descriptions. Re-loading the page costs zero API calls.
2. **Prompt cap** — at most `MaxCatalogSkills` (60) skills go into a prompt.
3. **Explanation cap** — only the top `ExplanationCount` (5) matches get an LLM sentence, and
   the whole feature can be switched off with `GenerateMatchExplanations: false`.
4. **Two calls maximum** per uncached run: one analysis, one explanation batch.
5. **Timeout** — `TimeoutSeconds` (15), after which the fallback takes over.

### Configuring the key

The key must **never** be committed. This module adds **nothing** to `appsettings.json` — every
option has a working default in code, so the shared config file stays untouched. Supply the key
out-of-band:

```bash
# option A — user secrets (preferred for local dev)
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "YOUR_KEY_FROM_AI_STUDIO"

# option B — environment variable
setx Gemini__ApiKey "YOUR_KEY_FROM_AI_STUDIO"
```

Get a free key at <https://aistudio.google.com/apikey>.

### Degradation path

| Condition | Behaviour |
|-----------|-----------|
| No API key set | Offline analyser; page shows an "Offline analysis" badge |
| Quota exhausted / HTTP error | Logged as a warning, offline analyser |
| Timeout | Logged, offline analyser |
| Malformed JSON | Logged with a snippet, offline analyser |
| `Gemini:Enabled = false` | Offline analyser, no network call at all |

The offline analyser matches on shared category plus Jaccard token overlap between skill names,
categories and descriptions. It is weaker than the model, but it always answers — so the
module never has a hard dependency on an external service.

---

## 7. Requirement traceability

### Acceptance Criteria (§9)

| # | Criterion | How it is met |
|---|-----------|---------------|
| 1 | Student must be logged in | `[Authorize]` on the controller |
| 2 | Must have ≥ 1 learning goal | Empty-goal guard returns the "add a goal" state |
| 3 | System analyses relevant skills | Stage A, on the student's own descriptions |
| 4 | Suitable peers identified | Stage B candidate discovery over goals ∪ related |
| 5 | Recommended peers displayed | `Views/Recommendations/Index.cshtml` |
| 6 | Match score **or** reason each | Both: score badge + reasons + AI one-liner |

### Non-functional requirements (§6)

| Requirement | How it is met |
|-------------|---------------|
| Security | `[Authorize]`; API key never in source control |
| Performance | Fixed query count regardless of candidate volume; scoring in memory; LLM cached |
| Usability | Bootstrap cards, plain-English reasons, expandable score breakdown |
| Reliability | Offline fallback; the LLM can never break the page |
| Privacy | Only `FullName` and `Bio` are exposed; no email; descriptions are sent to the LLM without identifiers |
| Scalability | Every query is filtered to the candidate set; weights tunable without redeploy |
| Maintainability | Interface-separated stages; provider swappable |

---

## 8. Known limitations / next steps

1. **Candidate scoring is in-memory.** Fine at class or department scale. Past a few thousand
   active mentors, the scoring loop should move to a SQL-side pre-filter, or the analysis should
   be precomputed on a schedule rather than per request.
2. **`IMemoryCache` is per-process.** A multi-instance deployment would want a distributed cache
   so the free-tier quota is shared rather than multiplied.
3. **Availability is not modelled.** Nothing in the current schema records when a student is free,
   so timetable fit cannot be scored. It would be a strong seventh signal.
4. **The "Send learning request" button is inert**, pending the Request module. The recommendation
   card is already carrying `MentorId` and the matched skill ids it will need.
5. **No feedback loop yet.** Once accept/reject data exists, acceptance rate per mentor would be a
   better reputation signal than star rating alone.

---

## 9. Ownership and integration footprint

This is one member's part of a group project. The module is written to be **additive**: nothing
another member wrote is edited, reordered or renumbered.

### Files this module adds (entirely owned here)

```
Services/RecommendationModuleExtensions.cs
Services/IRecommendationService.cs
Services/RecommendationService.cs
Services/RecommendationOptions.cs
Services/AI/ISkillAnalysisService.cs
Services/AI/GeminiSkillAnalysisService.cs
Services/AI/GeminiClient.cs
Services/AI/GeminiOptions.cs
Services/AI/SkillAnalysisModels.cs
Models/ViewModels/RecommendationViewModels.cs
Controllers/RecommendationsController.cs
Views/Recommendations/Index.cshtml
docs/AI-Recommendation-Module-Plan.md
```

### The only shared file touched

`Program.cs` — **4 inserted lines, 0 deletions, 0 modified lines:**

```csharp
using AIstudentskillexchange.Services;                        // with the other usings

// AI Recommendation Module (peer recommendations + AI skill analysis)
builder.Services.AddAiRecommendationModule(builder.Configuration);
```

All registration detail lives in `RecommendationModuleExtensions.cs`, so this stays a one-line
call however the module grows. If it conflicts on merge, the resolution is always "keep both".

### Explicitly not touched

| File / area | Why |
|---|---|
| `Models/*.cs` | Entities are another member's part — see §5.4 |
| `Migrations/**`, `ApplicationDbContextModelSnapshot.cs` | No schema change, so no migration conflict |
| `Data/ApplicationDbContext.cs` | Module only queries existing `DbSet`s |
| `appsettings.json` | All options default in code |
| `Views/Shared/_Layout.cshtml` | No nav entry added — see below |
| `Controllers/HomeController.cs`, `Views/Home/**` | Not this module's part |

### Reaching the page

Because `_Layout.cshtml` is shared and owned by whoever builds the site navigation, **no nav link
is added**. The module is reachable directly at **`/Recommendations`**.

When the navigation owner is ready, this is the line to add — their call, their file:

```html
<li class="nav-item">
    <a class="nav-link text-dark" asp-area="" asp-controller="Recommendations" asp-action="Index">Recommended Peers</a>
</li>
```

---

## 10. How to run

```bash
dotnet run
```

No migration and no `dotnet ef database update` is needed — this module adds no schema change.

Then sign in, add at least one "want to learn" skill with a description, and open
**Recommended Peers** in the nav bar. The badge at the top right of the page shows whether
the results came from the LLM or the offline fallback.
