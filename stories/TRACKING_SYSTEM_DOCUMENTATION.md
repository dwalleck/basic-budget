# Story Tracking System Documentation

## Overview

A dependency-aware, atomic story tracking system for managing complex software implementation projects. This system breaks down large features into small, self-contained units of work with clear dependencies, ensuring developers always know what to work on next.

## Core Philosophy

### 1. Atomic Stories

- Each story represents the smallest possible unit of shippable work
- Stories are self-contained with all context needed for implementation
- No story should take more than 1-2 days to complete
- Each story results in a pull request

### 2. Dependency-Driven Workflow

- Stories explicitly declare what blocks them and what they block
- The system automatically determines what can be worked on based on completed dependencies
- Prevents wasted work on blocked tasks
- Creates natural implementation order

### 3. Complete Context

- Each story contains everything needed to implement it
- No need to search through other documents
- Includes architecture requirements, code examples, test cases
- References specific files and line numbers

## System Components

### 1. Directory Structure

```
stories/
├── INDEX.md                    # Master tracking document with all stories
├── STORY_TEMPLATE.md           # Template for creating new stories
├── CURRENT_STATUS.md           # Real-time status dashboard
├── GENERATE_STORIES.md         # Guide for creating new stories
├── dashboard.html              # Visual HTML dashboard
├── check-next.sh              # CLI tool to find next story
├── queries/                   # Query operation stories
├── mutations/                 # Mutation operation stories
├── types/                     # Type definition stories
├── subscriptions/            # Subscription stories
└── infrastructure/           # Infrastructure stories
```

### 2. Story Structure

Each story follows this template:

```markdown
# Story: [STORY_ID] - [TITLE]

## Status
- [ ] Not Started
- [ ] In Progress
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
[Brief description of what needs to be implemented]

## Acceptance Criteria
- [ ] [Specific measurable outcome 1]
- [ ] [Specific measurable outcome 2]
- [ ] [All code builds successfully]
- [ ] [Pull request opened when complete]

## Technical Context
### Architecture Requirements
[Architecture patterns to follow]

### Implementation Location
[Exact file paths where code goes]

### Related Files
[Files to reference or modify]

### Schema/API Reference
[Relevant specifications]

## Implementation Steps
### 1. Create Feature Branch
### 2. [Layer-by-layer implementation]
### 3. Verify & Test
### 4. Create Pull Request

## Dependencies
- **Blocked By**: [Stories that must complete first]
- **Blocks**: [Stories that depend on this]

## Definition of Done
[Final checklist]
```

### 3. Story Identification Pattern

**Format**: `[CATEGORY]-[NUMBER]`

Categories:

- `FOUND-XXX`: Foundation stories (must complete first)
- `TYPE-XXX`: Type definitions
- `QUERY-XXX`: Query operations
- `MUT-XXX`: Mutation operations
- `SUB-XXX`: Subscription operations
- `INFRA-XXX`: Infrastructure stories

### 4. Tracking Tools

#### A. Command Line Tool (`check-next.sh`)

```bash
#!/bin/bash
# Finds next available story based on:
# 1. What's not started
# 2. What's not blocked
# 3. Priority order
```

#### B. Status Dashboard (`CURRENT_STATUS.md`)

```markdown
## 🎯 WORK ON THIS NEXT
[Always shows the single next story]

## 📊 Progress Overview
[Statistics and progress bars]

## 🚀 Ready to Start
[Unblocked stories]

## ⏸️ Blocked Stories
[What's waiting on dependencies]
```

#### C. Visual Dashboard (`dashboard.html`)

- Web-based visual representation
- Progress bars and statistics
- Copy-paste commands
- Color-coded priorities

### 5. Master Index (`INDEX.md`)

- Complete list of all stories
- Dependency graph
- Implementation phases
- Status tracking

## Key Features

### 1. Dependency Management

```yaml
Dependencies:
  Blocked By: [FOUND-001, TYPE-004]
  Blocks: [QUERY-006, QUERY-007]
```

- Prevents starting work that will be blocked
- Shows impact of completing current work
- Creates natural work order

### 2. Branch Strategy

Every story includes:

```bash
git checkout -b story/[STORY_ID]-[brief-description]
```

- Consistent branch naming
- Easy to track story progress in git
- Clear PR association

### 3. Architecture Enforcement

Each story includes:

- Architecture requirements section
- Layer-specific implementation guidance
- Pattern enforcement (e.g., hexagonal architecture)
- Error handling patterns

### 4. Progress Tracking

Multiple status checkpoints:

1. Not Started
2. In Progress
3. Code Complete
4. PR Opened
5. Merged

### 5. Self-Contained Context

Each story contains:

- Complete implementation steps
- Code examples
- Test cases
- File locations
- Related documentation references

## Implementation Guide

### Setting Up the System

1. **Create Directory Structure**

```bash
mkdir -p stories/{queries,mutations,types,subscriptions,infrastructure}
```

2. **Create Story Template**
Copy the STORY_TEMPLATE.md with your project-specific requirements

3. **Generate Initial Stories**

- Break down your specification into atomic units
- Identify dependencies between units
- Create story files following the template

4. **Create Tracking Tools**

- Adapt check-next.sh for your needs
- Create CURRENT_STATUS.md
- Optionally create dashboard.html

### Creating a New Story

1. **Identify Atomic Unit**

- Can it be implemented independently?
- Is it small enough for 1-2 days?
- Does it have clear acceptance criteria?

2. **Determine Dependencies**

- What must be complete before this?
- What does this block?

3. **Fill Template**

- Use consistent story ID format
- Include all context
- Add specific implementation steps
- Reference exact files

4. **Add to Index**

- Update INDEX.md
- Update dependency graph
- Assign to appropriate phase

### Working on a Story

1. **Check What's Next**

```bash
./stories/check-next.sh
```

2. **Review Story**

```bash
cat stories/[category]/[STORY-ID].md
```

3. **Create Branch**

```bash
git checkout -b story/[STORY-ID]-[description]
```

4. **Implement**

- Follow story's implementation steps
- Check off acceptance criteria
- Ensure all code builds

5. **Update Status**

- Mark story as "In Progress" when starting
- Mark "Code Complete" when done
- Mark "PR Opened" after creating PR
- Mark "Merged" after PR approval

## Benefits

### For Individual Developers

- Always know what to work on next
- Never blocked by unclear requirements
- Complete context in one place
- Clear definition of done

### For Teams

- Parallel work on non-dependent stories
- Clear communication about blockers
- Consistent implementation patterns
- Easy onboarding for new developers

### For Project Management

- Accurate progress tracking
- Dependency visibility
- Risk identification (blocked work)
- Predictable delivery order

## Customization Points

### 1. Story Categories

Adapt categories to your project:

- API endpoints
- Database migrations
- UI components
- Business features

### 2. Status Workflow

Customize status checkpoints:

- Add "In Review" status
- Add "Testing" status
- Add "Deployed" status

### 3. Architecture Requirements

Include your specific patterns:

- MVC, MVVM, etc.
- Testing requirements
- Documentation standards
- Code review checklist

### 4. Dependency Rules

Define your dependency patterns:

- Database before API
- API before UI
- Types before operations

## Example Use Cases

### GraphQL API Implementation

- Foundation (scalars, error handling)
- Types (models, inputs, payloads)
- Operations (queries, mutations, subscriptions)
- Infrastructure (caching, transport)

### Microservice Development

- Service contracts
- Domain models
- Business logic
- API endpoints
- Integration points

### UI Component Library

- Design tokens
- Atomic components
- Composite components
- Page layouts
- Integration examples

## Anti-Patterns to Avoid

1. **Stories Too Large**

- If it takes more than 2 days, break it down
- If it touches too many files, split it

2. **Missing Dependencies**

- Always explicitly list what blocks the story
- Update dependencies as you discover them

3. **Incomplete Context**

- Don't assume knowledge
- Include all necessary information
- Reference specific files and line numbers

4. **Skipping Status Updates**

- Update status as you progress
- Keeps the system accurate
- Helps team coordination

## Tools Integration

### Git Integration

```bash
# Branch naming from story
git checkout -b story/[STORY-ID]

# Commit message format
git commit -m "feat: [STORY-ID] - Description"

# PR title format
"[STORY-ID] - Story Title"
```

### CI/CD Integration

- Use story ID in build tags
- Link deployments to stories
- Track story completion in pipelines

### Project Management Tools

- Export story list to Jira/GitHub Issues
- Sync status with project boards
- Generate burndown charts from completion

## Maintenance

### Regular Updates

1. Update CURRENT_STATUS.md daily
2. Review blocked stories weekly
3. Archive completed stories monthly

### Story Refinement

- Split stories that prove too large
- Merge stories that are too small
- Update dependencies as discovered
- Add learnings to templates

### System Evolution

- Adapt categories as project grows
- Refine status workflow
- Improve templates based on usage
- Add automation where helpful

## Migration Path

### From Existing Project

1. List all pending work
2. Break into atomic units
3. Identify dependencies
4. Create stories for each unit
5. Start with blocked work first

### To Other Projects

1. Copy directory structure
2. Adapt story template
3. Customize categories
4. Update architecture requirements
5. Generate project-specific stories

## Conclusion

This tracking system provides a robust, dependency-aware way to manage complex implementation projects. By breaking work into atomic, self-contained stories with clear dependencies, it ensures developers always know what to work on next while maintaining architectural consistency and enabling accurate progress tracking.

The system is intentionally simple (markdown files and basic scripts) to ensure portability, version control friendliness, and ease of customization for different projects and teams.
