# Email Cleanup Rules Audit & Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clean and optimize the MongoDB `cleanup_rules` collection to eliminate false-positive risks (such as trashing GitHub PR notifications and work shifts), deduplicate fragmented rules, switch newsletters to Archive, resolve the N+1 Gmail API query bottleneck in `RunCleanupCommandHandler`, improve the Gemini spam detection prompt, and fix regex escape token normalization in `EmailSafetyRules`.

**Architecture:** 
1. Database maintenance script to sanitize and deduplicate rules in MongoDB.
2. Group-by-query batching in `RunCleanupCommandHandler` to reduce N Gmail API calls to 1 call per unique query.
3. Enhanced regex normalization in `EmailSafetyRules.AreRegexPatternsSimilar` to handle `\s`, `\d`, `\w`, `\b` escape sequences.
4. Upgraded AI prompt in `GeminiAIService.AnalyzeSpamPatternsAsync` to guide Archive vs Trash and protect platform notification domains.

**Tech Stack:** C# .NET 8, MongoDB.Driver, Python / Pymongo for migration, xUnit, FluentAssertions, NSubstitute.

---

### Task 1: Fix Regex Normalization in `EmailSafetyRules`

**Files:**
- Modify: `src/backend/GOpsHub.Application/Features/EmailOps/EmailSafetyRules.cs`
- Test: `src/backend/GOpsHub.Tests/Unit/EmailSafetyRulesTests.cs`

- [ ] **Step 1: Write failing unit test for `\s` and `\d` similarity in `EmailSafetyRulesTests`**
- [ ] **Step 2: Run test to verify failure**
- [ ] **Step 3: Update `Clean` and `AreRegexPatternsSimilar` in `EmailSafetyRules.cs` to handle escape sequences properly**
- [ ] **Step 4: Run test to verify it passes**
- [ ] **Step 5: Commit changes**

---

### Task 2: Optimize `RunCleanupCommandHandler` to Eliminate N+1 Gmail API Queries

**Files:**
- Modify: `src/backend/GOpsHub.Application/Features/EmailOps/Commands/CleanupCommands.cs`
- Test: `src/backend/GOpsHub.Tests/Unit/RunCleanupCommandHandlerTests.cs`

- [ ] **Step 1: Write unit test in `RunCleanupCommandHandlerTests` verifying `GetEmailsAsync` is called once per distinct query**
- [ ] **Step 2: Run test to verify failure**
- [ ] **Step 3: Refactor `RunCleanupCommandHandler` to group rules by `BuildGmailQuery(rule)` and deduplicate processed email IDs in memory**
- [ ] **Step 4: Run test to verify it passes**
- [ ] **Step 5: Commit changes**

---

### Task 3: Improve AI Spam & Newsletter Detection Prompt in `GeminiAIService`

**Files:**
- Modify: `src/backend/GOpsHub.Infrastructure/AI/GeminiAIService.cs`

- [ ] **Step 1: Update prompt in `AnalyzeSpamPatternsAsync` to mandate `Archive` for newsletters/updates, forbid targeting platform notification senders (`notifications@github.com`, etc.), and require `\b` word boundaries for short keywords**
- [ ] **Step 2: Run all backend tests to ensure zero regressions**
- [ ] **Step 3: Commit changes**

---

### Task 4: Sanitize and Deduplicate MongoDB `cleanup_rules`

**Files:**
- Create & Execute: Scratch Python script to update MongoDB `cleanup_rules`
- Verify: Query DB and inspect updated rules

- [ ] **Step 1: Remove or deactivate dangerous GitHub PR rule `6ab2de56ec3217f352afa280`**
- [ ] **Step 2: Set WorkBridge rule `6aa522aec47cd974e7b36977` to `isActive: False` and `Action: Archive`**
- [ ] **Step 3: Deduplicate redundant rules (LinkedIn, HackerNoon, Microsoft Ads, StackOverflow)**
- [ ] **Step 4: Switch newsletters/updates/digest rules from Trash (`0`) to Archive (`1`)**
- [ ] **Step 5: Fix loose regex keywords with word boundaries `\b`**
- [ ] **Step 6: Verify DB state and test rules count**

---

### Task 5: Final Verification & Integration

- [ ] **Step 1: Run `dotnet test` on all backend projects**
- [ ] **Step 2: Verify git status and prepare clean commits**
