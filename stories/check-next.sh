#!/bin/bash

# Simple script to find the next story to work on
# Usage: ./stories/check-next.sh

echo "🎯 BasicBudget - Next Story Checker"
echo "===================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Find stories that are in progress
echo -e "${YELLOW}📍 Currently In Progress:${NC}"
in_progress=$(grep -l "^\- \[x\] In Progress" stories/**/*.md 2>/dev/null)
if [ -z "$in_progress" ]; then
    echo "  None"
else
    for file in $in_progress; do
        story_id=$(basename "$file" .md)
        echo -e "  ${GREEN}✓${NC} $story_id"
        echo "    File: $file"
    done
fi
echo ""

# Find the next ready story
echo -e "${BLUE}🚀 Next Story to Start:${NC}"

# Check FOUND-001 first (highest priority)
if grep -q "^\- \[ \] Not Started" stories/types/FOUND-001-configure-scalars.md 2>/dev/null; then
    echo -e "  ${GREEN}➡️${NC} FOUND-001 - Configure GraphQL Scalar Types"
    echo "    Priority: CRITICAL (blocks all other stories)"
    echo "    File: stories/types/FOUND-001-configure-scalars.md"
    echo "    Time Estimate: 2 hours"
    echo ""
    echo "  Start with:"
    echo "    git checkout -b story/FOUND-001-configure-scalars"
    echo "    cat stories/types/FOUND-001-configure-scalars.md"
elif grep -q "^\- \[ \] Not Started" stories/types/FOUND-002-error-handling.md 2>/dev/null; then
    echo -e "  ${GREEN}➡️${NC} FOUND-002 - Setup Error Handling Infrastructure"
    echo "    Priority: HIGH (blocks all mutations)"
    echo "    File: stories/types/FOUND-002-error-handling.md"
    echo "    Time Estimate: 3 hours"
    echo ""
    echo "  Start with:"
    echo "    git checkout -b story/FOUND-002-error-handling"
    echo "    cat stories/types/FOUND-002-error-handling.md"
else
    # Find any other not started stories
    not_started=$(grep -l "^\- \[ \] Not Started" stories/**/*.md 2>/dev/null | head -1)
    if [ -n "$not_started" ]; then
        story_id=$(basename "$not_started" .md)
        echo -e "  ${GREEN}➡️${NC} $story_id"
        echo "    File: $not_started"
        echo ""
        echo "  Start with:"
        echo "    git checkout -b story/$story_id"
        echo "    cat $not_started"
    else
        echo -e "  ${GREEN}✅${NC} All stories are either completed or in progress!"
    fi
fi
echo ""

# Show quick stats
echo -e "${YELLOW}📊 Quick Stats:${NC}"
total=$(find stories -name "*.md" -not -name "STORY_TEMPLATE.md" -not -name "INDEX.md" -not -name "CURRENT_STATUS.md" -not -name "GENERATE_STORIES.md" | wc -l)
completed=$(grep -l "^\- \[x\] Merged" stories/**/*.md 2>/dev/null | wc -l)
in_progress_count=$(echo "$in_progress" | grep -c "^" 2>/dev/null || echo 0)
ready=$((total - completed - in_progress_count))

echo "  Total Stories: $total"
echo "  Completed: $completed"
echo "  In Progress: $in_progress_count"
echo "  Ready to Start: $ready"
echo ""

# Show help
echo -e "${BLUE}💡 Helpful Commands:${NC}"
echo "  View current status:     cat stories/CURRENT_STATUS.md"
echo "  View story index:        cat stories/INDEX.md"
echo "  Check specific story:    cat stories/[category]/[STORY-ID].md"
echo "  Update story status:     Edit the story file and check/uncheck boxes"
echo ""