# Story Tracking System - Quick Start Guide

## 🚀 Start Using in 5 Minutes

### 1. Check What to Work on Next
```bash
./stories/check-next.sh
```
This immediately tells you the next story to implement.

### 2. View the Story
```bash
cat stories/types/FOUND-001-configure-scalars.md
```
Everything you need is in the story file.

### 3. Start Working
```bash
git checkout -b story/FOUND-001-configure-scalars
```
Follow the implementation steps in the story.

### 4. Track Progress
Edit the story file and check boxes as you complete:
- [x] In Progress ← Check this when starting
- [x] Code Complete ← Check when implementation done
- [x] PR Opened ← Check after creating PR

### 5. Find Next Story
```bash
./stories/check-next.sh
```
The system automatically shows the next unblocked story.

---

## 📁 What's in Each Story?

Every story contains:
- **Overview**: What you're building
- **Acceptance Criteria**: Definition of done
- **Technical Context**: Architecture requirements
- **Implementation Steps**: Exact steps with code examples
- **Dependencies**: What must be done first
- **Test Examples**: How to verify it works

---

## 📊 Track Overall Progress

### Option 1: Command Line
```bash
cat stories/CURRENT_STATUS.md
```

### Option 2: Visual Dashboard
```bash
open stories/dashboard.html  # Mac
xdg-open stories/dashboard.html  # Linux
```

### Option 3: Master Index
```bash
cat stories/INDEX.md
```

---

## 🔄 Workflow Example

```bash
# 1. Check what's next
./stories/check-next.sh
# Output: "Work on FOUND-001 - Configure Scalars"

# 2. Read the story
cat stories/types/FOUND-001-configure-scalars.md

# 3. Create branch
git checkout -b story/FOUND-001-configure-scalars

# 4. Implement (following story steps)
# ... code ...

# 5. Verify
dotnet build
dotnet test

# 6. Create PR
git add .
git commit -m "feat: FOUND-001 - Configure GraphQL scalars"
git push origin story/FOUND-001-configure-scalars
gh pr create --title "FOUND-001 - Configure Scalars"

# 7. Update story status
# Edit the story file, check [x] PR Opened

# 8. Get next story
./stories/check-next.sh
# Output: "Work on FOUND-002 - Error Handling"
```

---

## 🎯 Key Concepts

### Dependencies Drive Order
- System knows what's blocked
- Always shows only unblocked work
- Completing stories unlocks new ones

### Everything is Self-Contained
- No searching for requirements
- No missing context
- No guessing implementation details

### Atomic Units
- Each story = one PR
- Small enough to complete in 1-2 days
- Clear definition of done

---

## 📝 Story Structure

```
STORY-ID format: [CATEGORY]-[NUMBER]
- FOUND: Foundation (do first!)
- TYPE: Type definitions
- QUERY: Query operations
- MUT: Mutations
- SUB: Subscriptions
- INFRA: Infrastructure
```

---

## 🚦 Status Progression

1. ⬜ Not Started
2. 🟦 In Progress (you're working on it)
3. 🟩 Code Complete (implementation done)
4. 🟨 PR Opened (awaiting review)
5. ✅ Merged (done!)

---

## 💡 Tips

- **Always start with foundation stories** (FOUND-*)
- **Check dependencies** before starting any story
- **Update status** as you progress
- **One story at a time** unless truly independent
- **Build everything** before marking complete

---

## 🆘 Troubleshooting

**Q: How do I know what's blocking a story?**
```bash
grep "Blocked By" stories/types/TYPE-001-*.md
```

**Q: How do I find all ready stories?**
```bash
grep -l "Not Started" stories/**/*.md | xargs grep -L "Blocked By.*\["
```

**Q: How do I see what a story unlocks?**
```bash
grep -l "Blocked By.*FOUND-001" stories/**/*.md
```

---

## 🎉 That's It!

You now know everything needed to use the story tracking system. The key command to remember:

```bash
./stories/check-next.sh
```

This always tells you what to work on next. The system handles the rest!