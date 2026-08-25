# Peer Discovery and Skill Matching Module — Design

**Project:** AI-Powered Student Skill Exchange and Peer Learning Platform
**Module:** Peer Discovery and Skill Matching (Requirement Analysis §11, *"Peer discovery → Search module"*)
**Covers:** student search and matching requirements

---

## 1. Scope

This module answers one student requirement from §5:

> **Student:** "Search for other students."

and delivers the *Search module* named in the §11 Requirement-to-Development mapping. It sits at
the **"Add Skills & Learning Goals → Send Learning Request"** point of the §10 workflow: the
student has a profile, and now needs to find a person.

### Search vs. recommendation

The platform has two different ways to find a partner, and this module is deliberately the
**pull** one:

| | Peer Discovery (this module) | AI Recommendation (separate module) |
|---|---|---|
| Who starts it | Student types a query | System pushes suggestions |
| Logic | Deterministic filters | Semantic / AI-assisted |
| Answers | "Show me Python teachers at Expert level" | "Here are people you'd click with" |
| Result order | Student chooses the sort | Ranked by model score |

A student who knows what they want should never have to read past a ranked list to get it. That
is what this module guarantees.

**Out of scope:** sending requests, scheduling, feedback, admin tools, AI ranking.

---

## 2. Requirements covered

| Requirement | Source | How it is met |
|---|---|---|
| Search for other students | §5 Student | Free-text + filtered search |
| Specify skill proficiency levels | §5 Student | Level is a search filter |
| Only authorised users | §6 Security | `[Authorize]` on the controller |
| Reasonable response time | §6 Performance | All filtering and paging in SQL |
| Simple and easy interface | §6 Usability | One filter bar, colour-coded match badges |
| Information only to authorised users | §6 Privacy | Only `FullName`, `Bio` and listed skills exposed — never email |
| Support growing numbers of students | §6 Scalability | Paged queries; in-memory work is per-page only |
| Organised into separate modules | §6 Maintainability | Self-contained service behind an interface |

---

## 3. Architecture

```
Views/PeerDiscovery/Index.cshtml   Views/PeerDiscovery/Profile.cshtml
                    ▲                          ▲
                    │  PeerSearchViewModel     │  PeerResultViewModel
                    │                          │
        Controllers/PeerDiscoveryController      [Authorize]
                    ▲
                    │
        Services/Search/IPeerSearchService
              (PeerSearchService)
                    ▼
        Data/ApplicationDbContext  (EF Core → SQL Server LocalDB)
```

### File inventory

| File | Role |
|------|------|
| `Services/Search/IPeerSearchService.cs` | Search contract |
| `Services/Search/PeerSearchService.cs` | Query building + skill matching |
| `Services/Search/PeerSearchOptions.cs` | Page size, weights, limits |
| `Services/Search/PeerSearchModuleExtensions.cs` | One-line DI registration |
| `Models/ViewModels/PeerSearch/PeerSearchViewModels.cs` | Criteria, results, paging |
| `Controllers/PeerDiscoveryController.cs` | `/PeerDiscovery`, `/Profile/{id}`, `/Api` |
| `Views/PeerDiscovery/Index.cshtml` | Search page |
| `Views/PeerDiscovery/Profile.cshtml` | Single peer profile |

---

## 4. Search requirements

### 4.1 What a student can search by

| Filter | Query key | Behaviour |
|---|---|---|
| Free text | `query` | Matches student name, bio, **or any skill name** on their profile |
| Skill | `skillId` | Peers who listed this specific skill |
| Category | `category` | Peers with any skill in this category |
| Level | `level` | Beginner / Intermediate / Expert |
| Teach or learn | `skillType` | Narrows the three filters above to teaching **or** learning |
| Matches my goals | `onlyMatchingMyGoals` | Only peers teaching something on my learning list |
| Wants my skills | `onlyWantingMySkills` | Only peers wanting something I teach |
| Sort | `sort` | Best match / Name / Most skills taught |
| Page | `page` | 1-based |

`skillType` acting as a *modifier* is the important detail: "Python" + "Can teach it" finds
tutors, while "Python" + "Wants to learn it" finds study partners. One filter set, both
directions.

### 4.2 Design decisions

**Criteria bound from the query string, not posted.** Every search is a shareable, bookmarkable
URL, the back button behaves, and paging links are ordinary `<a>` tags. A student can send a
classmate a link to "all Expert React teachers".

**Students with an empty profile are excluded** (`u.StudentSkills.Any()`). A result you can
learn nothing about is noise.

**Page number is clamped to what exists.** A stale bookmark pointing at page 9 of a result set
that shrank to 3 pages shows page 3, not an empty screen.

**A short query is ignored, not rejected.** Under `MinimumQueryLength` (2) the text filter is
skipped rather than erroring, so a stray keystroke does not blank the page.

---

## 5. Skill matching requirements

Search finds *people*; matching explains *what each one is for*.

### 5.1 The three overlaps

For viewer **V** and result peer **P**, comparing `StudentSkill` rows:

| Overlap | Definition | Meaning |
|---|---|---|
| `TeachesWhatIWant` | P.ToTeach ∩ V.ToLearn | They can teach me |
| `WantsWhatICanTeach` | P.ToLearn ∩ V.ToTeach | I can teach them |
| `SharedGoals` | P.ToLearn ∩ V.ToLearn | We're learning the same thing |

### 5.2 Match classification

Evaluated in priority order — the first match wins:

| Match type | Condition | Badge |
|---|---|---|
| **Exchange partner** | teaches what I want **AND** wants what I teach | green |
| **Can teach you** | teaches what I want | blue |
| **Wants to learn from you** | wants what I teach | light blue |
| **Study buddy** | shares a learning goal | amber |
| No direct overlap | none of the above | grey |

**Exchange partner ranks highest deliberately.** §2 of the Requirement Analysis states the goal
as students *exchanging* knowledge, not one-way tutoring. A reciprocal pair is the outcome the
platform exists to create, so the UI names it first and colours it strongest.

### 5.3 Match strength (0–100)

Each overlap is scored as a fraction of **what the viewer actually listed**, then weighted:

```
strength =  min(1, |TeachesWhatIWant|  / |my goals|)  × 50
          + min(1, |WantsWhatICanTeach| / |my skills|) × 35
          + min(1, |SharedGoals|        / |my goals|)  × 15
```

Using the viewer's own list as the denominator is what makes the number meaningful: a peer who
covers **both** of your two goals scores higher than one covering two of your ten. Absolute
counts would reward students who simply listed many skills.

Weights live in `PeerSearchOptions` and are tunable without recompiling.

This is intentionally plain arithmetic — no model, no training data. A student can be told
exactly why they saw 65%, and the module has no external dependency to fail.

---

## 6. Data flow

```
Student submits the filter bar  →  GET /PeerDiscovery?query=...&skillId=...
   │
   ├─(1) Load viewer profile
   │     StudentSkills WHERE StudentId = me   →  myGoalIds, myTeachIds
   │
   ├─(2) BuildQuery  ── every filter applied as SQL
   │     Users WHERE Id <> me AND StudentSkills.Any()
   │           [+ text / skill / category / level / type / matching filters]
   │
   ├─(3) COUNT(*)                       → total, total pages
   │     clamp requested page to range
   │
   ├─(4) ApplySort + Skip/Take          → ONE page of students
   │
   ├─(5) Load StudentSkills WHERE StudentId IN (that page only)
   │
   ├─(6) Per result, in memory:
   │        classify the three overlaps
   │        → MatchType + MatchStrength
   │
   └─(7) Render cards, grouped by overlap, with paging links
```

### Why the order matters

Filtering and counting happen **in SQL, before paging**. Doing it the other way — fetch
students, then filter in C# — would make the result count wrong and load the whole student table
into memory. Only step 6 runs in memory, over at most `PageSize` (10) students, so cost stays
flat as the student body grows.

### Entities read

| Entity | Fields used | Purpose |
|---|---|---|
| `ApplicationUser` | `Id`, `FullName`, `Bio` | Identity and display |
| `StudentSkill` | `StudentId`, `SkillId`, `Type`, `Level` | Filtering and matching |
| `Skill` | `Id`, `Name`, `Category` | Filter dropdowns, display |

**No schema change.** This module adds no migration and does not touch
`ApplicationDbContextModelSnapshot.cs`, so it cannot conflict with migration work by other
members.

---

## 7. Ownership and integration footprint

This is one member's part of a group project, written to be **additive** — nothing another
member wrote is edited, reordered or renumbered.

### Files this module adds (entirely owned here)

```
Services/Search/IPeerSearchService.cs
Services/Search/PeerSearchService.cs
Services/Search/PeerSearchOptions.cs
Services/Search/PeerSearchModuleExtensions.cs
Models/ViewModels/PeerSearch/PeerSearchViewModels.cs
Controllers/PeerDiscoveryController.cs
Views/PeerDiscovery/Index.cshtml
Views/PeerDiscovery/Profile.cshtml
docs/Peer-Discovery-And-Skill-Matching-Plan.md
```

### The only shared file touched

`Program.cs` — **3 inserted lines, 0 deletions, 0 modified lines:**

```csharp
// Peer Discovery and Skill Matching Module (student search + skill matching)
builder.Services.AddPeerDiscoveryModule(builder.Configuration);
```

**No `using` directive is added.** `PeerSearchModuleExtensions` is declared in the
`Microsoft.Extensions.DependencyInjection` namespace — the standard .NET convention for
DI extension methods, already imported implicitly by the web SDK. That matters for more than
tidiness: the using-block is four lines long and every member's branch would otherwise insert a
line at the same spot, producing a guaranteed merge conflict. Avoiding the using entirely means
this branch's only Program.cs edit is one self-contained block in its own location.

*Verified:* a trial merge of this branch with the AI Recommendation branch auto-merges with no
conflict, and the merged result compiles clean.

### Deliberately not touched

| File / area | Why |
|---|---|
| `Models/*.cs`, `Data/ApplicationDbContext.cs` | Entities are another member's part; this module only queries them |
| `Migrations/**`, `ApplicationDbContextModelSnapshot.cs` | No schema change |
| `appsettings.json` | All options default in code |
| `Views/Shared/_Layout.cshtml` | Navigation is owned by another member — see below |
| `Controllers/HomeController.cs`, `Views/Home/**` | Not this module's part |

### Reaching the page

No nav link is added, because `_Layout.cshtml` is shared. The module is reachable at
**`/PeerDiscovery`**. When the navigation owner is ready, this is the line to add — their file,
their call:

```html
<li class="nav-item">
    <a class="nav-link text-dark" asp-area="" asp-controller="PeerDiscovery" asp-action="Index">Find Peers</a>
</li>
```

---

## 8. Known limitations / next steps

1. **`Contains` search does not use an index.** It translates to SQL `LIKE '%term%'`, which scans.
   Fine at class or department scale; a larger deployment wants SQL Server full-text search.
2. **"Best match" is refined per page, not globally.** SQL orders by overlap counts, then the
   page is re-sorted by blended strength. Ordering is therefore exact within a page and very
   close across pages. Exact global ordering would need the strength computed in SQL.
3. **No availability or timetable filter** — the schema records no availability, so students
   cannot yet filter by "free on Tuesday evenings".
4. **The "Send learning request" button is inert**, pending the Request module. Both views
   already carry the `StudentId` and matched skill ids it will need.
5. **No saved searches or alerts.** A natural follow-up: let a student save "Expert Python
   teachers" and be notified when someone new matches.

---

## 9. How to run

```bash
dotnet run
```

No migration and no `dotnet ef database update` is needed — this module adds no schema change.

Sign in and open **`/PeerDiscovery`**. Add skills to your own profile first to see match badges
and overlap percentages; the search itself works with an empty profile.
