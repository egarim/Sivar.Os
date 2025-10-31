# Comment Reply System - Testing Guide

## Overview
This document provides a comprehensive testing guide for the Instagram-style comment reply system implemented in PR `copilot/improve-comment-reply-system`.

## Implementation Summary

### What Was Implemented

**Backend:**
- Updated `CommentsClient` (server-side) to properly implement `GetCommentRepliesAsync`
- All other backend infrastructure was already in place (service, repository, DTOs, API endpoints)

**Frontend:**
1. **ReplyInput Component** (`Sivar.Os.Client/Components/Feed/ReplyInput.razor`)
   - Instagram-style inline reply input
   - Auto-focus on mount
   - Character counter (shows when >1900 chars)
   - Post/Cancel buttons
   - Mention username in placeholder

2. **CommentItem Component** (Updated `Sivar.Os.Client/Components/Feed/CommentItem.razor`)
   - Complete redesign following Instagram pattern
   - Inline header layout (username · time · actions)
   - Reply button
   - "View replies (N)" toggle with line prefix
   - Lazy loading of replies
   - Recursive rendering (supports nested replies)
   - Depth tracking (max 2 levels)
   - Optimistic updates

3. **CommentSection Component** (Updated `Sivar.Os.Client/Components/Feed/CommentSection.razor`)
   - Passes Depth=0 and MaxDepth=2 to comments
   - Handles reply creation callbacks
   - Updates total comment count

4. **Styling** (`Sivar.Os/wwwroot/css/comment-replies.css`)
   - Depth-based indentation (0px, 40px, 80px)
   - Instagram-style visual design
   - Smooth animations
   - Mobile responsiveness

## Testing Instructions

### Prerequisites
1. Ensure the application is running locally
2. Have at least one post with some comments
3. Be logged in with a valid user account

### Test Cases

#### TC1: Create Top-Level Comment
1. Navigate to a post
2. Expand comments section
3. Type a comment in the main input field
4. Click "Send"
5. **Expected:** Comment appears at the top of the list

#### TC2: Show Reply Input
1. Find any comment
2. Click the "Reply" button
3. **Expected:**
   - Reply input appears inline below the comment
   - Input is auto-focused
   - Placeholder shows "Reply to @username..."
   - Post and Cancel buttons are visible

#### TC3: Create Reply (Level 1)
1. Click "Reply" on a top-level comment
2. Type a reply message
3. Click "Post"
4. **Expected:**
   - Reply is created immediately (optimistic update)
   - Reply input disappears
   - Reply count shows "— View replies (1)"
   - Replies section is automatically expanded
   - Reply appears indented 40px from left

#### TC4: View/Hide Replies
1. Find a comment with replies (shows "— View replies (N)")
2. Click "— View replies (N)"
3. **Expected:**
   - Replies expand with smooth animation
   - Replies are indented 40px
   - Button changes to "Hide replies" with collapse icon
4. Click "Hide replies"
5. **Expected:**
   - Replies collapse
   - Button changes back to "— View replies (N)"

#### TC5: Create Nested Reply (Level 2)
1. Expand a comment's replies
2. Click "Reply" on one of the replies
3. Type a nested reply
4. Click "Post"
5. **Expected:**
   - Nested reply appears indented 80px from left
   - Has left border for visual distinction
   - Shows correct depth treatment

#### TC6: Lazy Loading
1. Find a comment without expanded replies
2. Observe network tab in browser dev tools
3. Click "View replies (N)"
4. **Expected:**
   - API call to `/api/comments/{id}/replies` is made ONLY when expanding
   - No replies loaded on initial page load

#### TC7: Reply Input Validation
1. Click "Reply" on any comment
2. Leave input empty
3. **Expected:** "Post" button is disabled
4. Type 1 character
5. **Expected:** "Post" button is enabled
6. Type 2000+ characters
7. **Expected:** Character counter appears showing limit

#### TC8: Cancel Reply
1. Click "Reply" on any comment
2. Type some text
3. Click "Cancel"
4. **Expected:**
   - Reply input disappears
   - Text is cleared
   - No API call is made

#### TC9: Delete Reply
1. Create a reply
2. Click the menu button (three dots) on your own reply
3. Click "Delete"
4. Confirm deletion
5. **Expected:**
   - Reply is deleted
   - Reply count decrements
   - Parent comment updates

#### TC10: Mobile Responsiveness
1. Resize browser to < 768px width
2. **Expected:**
   - Reply indentation reduces to 32px/64px
   - Font sizes adjust (13px instead of 14px)
   - All features still work
   - Layout remains readable

#### TC11: Visual Design (Instagram Pattern)
1. Inspect any comment with replies
2. **Expected:**
   - Username, time, and actions are on one line
   - Time separator is "·" character
   - "View replies" button has "—" line prefix
   - Avatar is 32px circle on left
   - Reply button is subtle, not prominent

#### TC12: Optimistic Updates
1. Click "Reply" on a comment
2. Type a reply and click "Post"
3. Watch carefully
4. **Expected:**
   - Reply appears IMMEDIATELY (before API response)
   - Reply count increments IMMEDIATELY
   - No loading spinner or delay
   - Reply updates with real data when API responds

### Performance Testing

#### PT1: Initial Load Performance
1. Navigate to a post with 50+ comments
2. Observe network tab
3. **Expected:**
   - Only top-level comments are loaded
   - No replies are loaded initially
   - Page loads quickly

#### PT2: Reply Loading Performance
1. Click "View replies" on multiple comments rapidly
2. **Expected:**
   - Each request is independent
   - No duplicate requests
   - Requests are cached (clicking hide/show doesn't re-fetch)

### Accessibility Testing

#### AT1: Keyboard Navigation
1. Use Tab key to navigate comments
2. **Expected:**
   - Can tab through all comments and replies
   - Reply button is keyboard accessible
   - View replies button is keyboard accessible
   - Menu buttons are keyboard accessible

#### AT2: Screen Reader
1. Enable screen reader (NVDA, JAWS, or VoiceOver)
2. Navigate through comments
3. **Expected:**
   - Comment content is announced
   - Reply counts are announced
   - Buttons have proper labels
   - Nesting level is indicated

### Browser Compatibility

Test on:
- [ ] Chrome/Edge (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Mobile Chrome
- [ ] Mobile Safari

## Known Limitations

1. **Max Depth:** Visual nesting is limited to 2 levels (Instagram pattern)
   - Deeper replies are still created but don't get additional indentation
   - This is by design per the implementation plan

2. **Real-time Updates:** Changes by other users are not reflected in real-time
   - Requires page refresh to see new replies from others
   - Future enhancement: SignalR integration

3. **Reply Editing:** Not implemented in this phase
   - Comments can be deleted but not edited
   - Future enhancement

4. **Pagination:** "Load more replies" is implemented but all replies load at once currently
   - Backend supports pagination
   - Frontend can be enhanced to load in batches

## Troubleshooting

### Issue: Reply button doesn't show input
**Solution:** Check browser console for JavaScript errors. Ensure MudBlazor is loaded.

### Issue: Replies don't expand
**Solution:** Check network tab - ensure API call to `/api/comments/{id}/replies` succeeds

### Issue: Reply count doesn't update
**Solution:** Check that optimistic update is working (should increment immediately). If not, check console for errors.

### Issue: CSS not applied
**Solution:** Verify `comment-replies.css` is referenced in `App.razor` and served from `/css/comment-replies.css`

### Issue: Indentation not showing
**Solution:** Check browser dev tools - verify CSS classes `depth-0`, `depth-1`, `depth-2` are applied

## Success Criteria

✅ All test cases (TC1-TC12) pass  
✅ All performance tests (PT1-PT2) pass  
✅ All accessibility tests (AT1-AT2) pass  
✅ Works on all target browsers  
✅ No console errors  
✅ No network errors  
✅ Follows Instagram visual design  
✅ Meets performance requirements (fast initial load, lazy loading)  

## Screenshots to Capture

Please take screenshots of:
1. Comment with "— View replies (1)" button
2. Expanded replies (showing indentation)
3. Nested replies (2 levels deep with border)
4. Reply input active state
5. Mobile view (<768px)
6. Full comment thread (multiple levels)

## Additional Notes

- All logging uses `ILogger` (check server logs for debugging)
- All components use MudBlazor (no raw HTML)
- Code follows DEVELOPMENT_RULES.md patterns
- Implementation follows COMMENT_REPLY_SYSTEM_IMPROVEMENT_PLAN.md

---

**Testing Status:** Ready for manual testing  
**Automated Tests:** Not included (per minimal changes requirement)  
**Documentation:** Complete  
**Code Review:** Pending
