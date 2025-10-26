# Browser Console & Network Monitoring Guide

## Quick Start
1. **Open DevTools**: Press `F12` (or right-click → Inspect)
2. **Go to Network Tab**: Click the "Network" tab
3. **Create a Post**: Try posting something
4. **Find the Request**: Look for `api/posts` POST request
5. **Check Response**: Should be `201 Created` with post data

---

## Network Tab Inspection

### What to Look For

#### 1. Request Headers
When you POST a post, check the Request Headers:

```
POST /api/posts HTTP/1.1
Host: localhost:5001
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
Content-Length: 125
```

✅ **Good**: `Authorization: Bearer ...` header is present  
❌ **Bad**: No Authorization header = not authenticated

#### 2. Request Payload
The POST body should look like:
```json
{
  "content": "This is my first post!",
  "visibility": "Public",
  "allowComments": true,
  "allowReactions": true,
  "scheduledDate": null,
  "tags": []
}
```

✅ **Good**: Content is not empty  
❌ **Bad**: Content is empty or null

#### 3. Response Status
```
Status: 201 Created        ✅ SUCCESS - Post created!
Status: 400 Bad Request    ❌ Validation failed
Status: 401 Unauthorized   ❌ Not authenticated
Status: 429 Too Many Req   ❌ Rate limited
Status: 500 Error          ❌ Server error
```

#### 4. Response Headers
Check these headers in the response:

```
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Content-Length: 1250
Location: /api/posts/550e8400-e29b-41d4-a716-446655440000
```

✅ **Good**: `201 Created` status with `Location` header  
❌ **Bad**: `400`, `401`, or `500` status

#### 5. Response Body
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "content": "This is my first post!",
  "visibility": "Public",
  "allowComments": true,
  "createdAt": "2025-10-25T10:30:45.123Z",
  "profile": {
    "id": "667f8400-e89b-12d3-a456-426614174111",
    "displayName": "John Doe",
    "username": "johndoe"
  }
}
```

✅ **Good**: Post object with all fields  
❌ **Bad**: Error message or empty response

---

## Console Tab Inspection

### JavaScript Logs to Expect

#### Success Message
```
✓ Post created successfully!
  ID: 550e8400-e29b-41d4-a716-446655440000
  Content: This is my first post!
  Author: John Doe
```

#### Error Messages
```
✗ Failed to create post!
  Status: 401
  Error: User not authenticated
```

or

```
✗ Error creating post
  Error: Network request failed
  Details: Failed to connect to POST /api/posts
```

### Common Console Errors to Ignore
- React warnings about keys
- CSS or font loading warnings
- These are NOT related to post creation

### What NOT to See
❌ `401 Unauthorized` - means Keycloak login failed  
❌ `CORS error` - means API call blocked by browser security  
❌ `TypeError: Cannot read properties of null` - means API didn't return post data

---

## Real-Time Testing Flow

### Step 1: Open DevTools
```
F12 → Network Tab
```

### Step 2: Create a Post
1. Find the Post Composer
2. Type: "Hello, this is a test post!"
3. Click "Post" button

### Step 3: Find the Request
- **Filter**: Type `api/posts` in the filter box
- **Look for**: Green POST request to `api/posts`

### Step 4: Inspect Request
- **Click**: The request row
- **View**: Request Headers tab
- **Look for**: 
  - ✅ `Authorization: Bearer ...`
  - ✅ `Content-Type: application/json`

### Step 5: Inspect Response
- **Click**: The request row (same one)
- **View**: Response tab
- **Check**: 
  - ✅ Status: `201 Created`
  - ✅ Body contains post ID and content
  - ✅ `profile` object with author info

---

## Troubleshooting via Network Tab

### Problem: POST shows 401 Unauthorized ❌

**Network Response**:
```json
{
  "error": "User not authenticated",
  "keycloakId": ""
}
```

**Causes**:
1. Not logged in to Keycloak
2. Session expired
3. JWT token not being sent

**Solution**:
1. Clear browser cache (Ctrl+Shift+Delete)
2. Log out and log in again
3. Check if Authorization header is present

### Problem: POST shows 400 Bad Request ❌

**Network Response Example 1**:
```json
{
  "error": "Post data is required"
}
```

**Cause**: Empty request body  
**Solution**: Check request payload in Request tab

**Network Response Example 2**:
```json
{
  "error": "Failed to create post - user or profile not found",
  "keycloakId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Cause**: User exists but profile doesn't  
**Solution**: User profile should auto-create on first login (Home.razor)

### Problem: POST shows 429 Too Many Requests ❌

**Network Response**:
```json
{
  "message": "Too many requests. Please try again later.",
  "remainingRequests": 0,
  "resetTime": "2025-10-25T10:35:00Z"
}
```

**Cause**: Rate limit exceeded (max 5 posts/minute)  
**Solution**: Wait for the resetTime

### Problem: POST shows 500 Internal Server Error ❌

**Network Response**:
```json
{
  "error": "Internal server error",
  "details": "Database connection failed"
}
```

**Cause**: Server-side error  
**Solution**: 
1. Check server console for detailed error logs
2. Check database connection
3. Restart application

---

## Advanced: Inspecting JWT Token

### Step 1: Find Authorization Header
In Network Tab → Request Headers:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Step 2: Copy the Token
Copy everything after `Bearer ` (the long string)

### Step 3: Decode at jwt.io
1. Go to https://jwt.io (official JWT decoder)
2. Paste your token
3. View the decoded claims

### Step 4: Look for Claims
In the **PAYLOAD** section, you should see:
```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "given_name": "John",
  "family_name": "Doe",
  "preferred_username": "johndoe",
  "iat": 1635169445,
  "exp": 1635255845
}
```

✅ **Good**: `sub` claim is present (this is your Keycloak ID)  
❌ **Bad**: `sub` claim is missing or empty

---

## Step-by-Step Success Checklist

Use this checklist when testing:

### Pre-Test
- [ ] Application running on https://localhost:5001
- [ ] Logged in with Keycloak
- [ ] DevTools open (F12)
- [ ] Network tab active

### During Test
- [ ] Type post content in composer
- [ ] Click "Post" button
- [ ] See POST request to `api/posts` in Network tab
- [ ] Request has `Authorization: Bearer ...` header
- [ ] Request body has content

### After Test
- [ ] Network response shows `201 Created`
- [ ] Response body has `id` and `profile` fields
- [ ] Post appears in your feed
- [ ] Console shows no error messages
- [ ] Browser console shows success (if implemented)

---

## Quick Reference: HTTP Status Codes

| Status | Meaning | Action |
|--------|---------|--------|
| **201** | Created ✅ | Post saved successfully |
| **400** | Bad Request ❌ | Invalid data, check payload |
| **401** | Unauthorized ❌ | Not logged in, login again |
| **404** | Not Found ❌ | API endpoint not found |
| **429** | Too Many Req ❌ | Rate limited, wait a moment |
| **500** | Server Error ❌ | Server crashed, check logs |

---

## Combining Server & Browser Monitoring

### Ideal Setup for Testing

**Terminal 1 (Server Logs)**:
```
Watch for: [CreatePost] ✅ SUCCESS
or: [CreatePost] ❌ FAILED
```

**Terminal 2 (Optional)**:
```
dotnet run with --verbose-logging (if available)
```

**Browser DevTools** (Network Tab):
```
Watch for: 201 Created response
Check request/response bodies
```

### Synchronized Testing
1. **Server Ready**: See "Application started" in terminal
2. **Create Post**: In browser
3. **Watch Server**: See detailed logs in terminal
4. **Check Network**: Verify response in DevTools
5. **Verify UI**: Post appears in feed
6. **Check DB**: (Optional) Query PostgreSQL directly

---

## Screenshots Reference (What to Look For)

### Good Request
```
REQUEST                           RESPONSE
────────────────────────         ─────────────────────
POST /api/posts                  ✅ 201 Created
Content-Type: application/json   Content-Length: 1250
Authorization: Bearer eyJ...     
Content-Length: 125              
                                 
{                                {
  "content": "Test post",          "id": "550e8...",
  "visibility": "Public"           "content": "Test post",
  ...                              "profile": {...},
}                                  ...
                                 }
```

### Bad Request (401)
```
REQUEST                           RESPONSE
────────────────────────         ──────────────────────
POST /api/posts                  ❌ 401 Unauthorized
Content-Type: application/json   Content-Length: 45
Authorization: (missing!)         
Content-Length: 125              
                                 {
{                                  "error": "User not 
  "content": "Test post",          authenticated"
  ...                              }
}
```

---

## Next: Server Console Monitoring

While doing network tab inspection, also watch the **server terminal** for logs starting with:

```
[CreatePost] POST request received at ...
[CreatePost] Step 1: Extracting Keycloak ID from request
[CreatePost] ✓ KeycloakId validated: ...
[CreatePost] ✅ SUCCESS: Post created with ID = ...
```

See `TESTING_POST_CREATION_WITH_LOGGING.md` for complete server log reference.

---

**Last Updated**: 2025-10-25  
**Testing Guide Version**: 1.0
