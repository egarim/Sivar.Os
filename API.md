# Sivar.Os API Documentation

**Base URL:** http://localhost:5001 (update with your domain)

## 🔐 Authentication

### Dev Login (Development Only)
```bash
POST /api/DevAuth/login
Content-Type: application/json

{
  "email": "test@sivar.os"
}
```

**Response:**
```json
{
  "success": true,
  "email": "test@sivar.os",
  "displayName": "test DevUser",
  "userId": "guid",
  "profileCount": 1,
  "message": "Development login successful"
}
```

### Check Auth Status
```bash
GET /api/DevAuth/status
```

### Logout
```bash
POST /api/DevAuth/logout
```

---

## 👤 Users & Profiles

### Get Current User
```bash
GET /api/Users/me
```

### Get User Profile
```bash
GET /api/Users/{userId}
```

### Update Profile
```bash
PUT /api/Users/profile
Content-Type: application/json

{
  "displayName": "John Doe",
  "bio": "Photographer",
  "contactEmail": "john@example.com",
  "contactPhone": "+503XXXXXXXX"
}
```

---

## 📝 Posts (Feed)

### Get Activity Feed
```bash
GET /api/Posts/activity-feed?page=1&pageSize=10
```

### Create Post
```bash
POST /api/Posts
Content-Type: application/json

{
  "content": "Check out my latest photo session!",
  "visibility": 0,
  "attachments": []
}
```

### Get Post by ID
```bash
GET /api/Posts/{postId}
```

### Update Post
```bash
PUT /api/Posts/{postId}
```

### Delete Post
```bash
DELETE /api/Posts/{postId}
```

---

## 💬 Comments

### Get Comments for Post
```bash
GET /api/Comments/{postId}
```

### Add Comment
```bash
POST /api/Comments
Content-Type: application/json

{
  "postId": "guid",
  "content": "Great photo!"
}
```

### Delete Comment
```bash
DELETE /api/Comments/{commentId}
```

---

## ❤️ Reactions

### Get Reactions for Post
```bash
GET /api/Reactions/{postId}
```

### Add Reaction
```bash
POST /api/Reactions
Content-Type: application/json

{
  "postId": "guid",
  "reactionType": "like"
}
```

**Reaction Types:** `like`, `love`, `wow`, `sad`, `angry`

### Remove Reaction
```bash
DELETE /api/Reactions/{postId}
```

---

## 📅 Resource Bookings (Photo Studio)

### Get Available Resources
```bash
GET /api/ResourceBookings/resources
```

### Get Bookings
```bash
GET /api/ResourceBookings?startDate=2026-02-17&endDate=2026-02-24
```

### Create Booking
```bash
POST /api/ResourceBookings
Content-Type: application/json

{
  "resourceId": "guid",
  "startTime": "2026-02-20T10:00:00Z",
  "endTime": "2026-02-20T11:00:00Z",
  "notes": "Family portrait session"
}
```

### Cancel Booking
```bash
DELETE /api/ResourceBookings/{bookingId}
```

---

## 🔔 Notifications

### Get User Notifications
```bash
GET /api/Notifications
```

### Mark as Read
```bash
POST /api/Notifications/{notificationId}/read
```

---

## 🔍 Search

### Search Everything
```bash
GET /api/Search?query=photographer&type=profiles&page=1&pageSize=10
```

**Search Types:** `all`, `profiles`, `posts`, `users`

---

## 👥 Followers

### Get Followers
```bash
GET /api/Followers/{profileId}
```

### Get Following
```bash
GET /api/Followers/{profileId}/following
```

### Follow Profile
```bash
POST /api/Followers/{profileId}/follow
```

### Unfollow Profile
```bash
DELETE /api/Followers/{profileId}/unfollow
```

---

## 📊 Analytics

### Get Profile Analytics
```bash
GET /api/Analytics/profile/{profileId}
```

### Get Post Analytics
```bash
GET /api/Analytics/post/{postId}
```

---

## 🏥 Health & Status

### Basic Health Check
```bash
GET /api/Health
```

### Detailed Health Check
```bash
GET /api/Health/detailed
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-02-17T10:00:00Z",
  "database": {
    "connected": true,
    "users": 5,
    "profiles": 5,
    "posts": 0
  }
}
```

---

## 📤 File Upload

### Upload File
```bash
POST /api/FileUpload
Content-Type: multipart/form-data

file: [binary]
```

**Note:** Azure Blob Storage needs to be configured for this to work.

---

## 🤖 ChatBot Settings (Future)

### Get Bot Settings
```bash
GET /api/ChatBotSettings/{profileId}
```

### Update Bot Settings
```bash
PUT /api/ChatBotSettings
```

---

## 🔑 Authentication Notes

- **Development:** Uses email-only login (no password)
- **Production:** Will use Keycloak OAuth2/OIDC
- **Cookies:** Session managed via ASP.NET Core cookie authentication
- **CORS:** Configured for same-origin by default

---

## 📋 Response Codes

- **200 OK** - Success
- **201 Created** - Resource created
- **204 No Content** - Success with no body
- **400 Bad Request** - Validation error
- **401 Unauthorized** - Not authenticated
- **403 Forbidden** - Not authorized
- **404 Not Found** - Resource not found
- **500 Internal Server Error** - Server error

---

## 🚀 Quick Test

Test the API is working:

```bash
# Check health
curl http://localhost:5001/api/Health

# Login
curl -X POST http://localhost:5001/api/DevAuth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@sivar.os"}' \
  --cookie-jar cookies.txt

# Get feed (requires login cookie)
curl http://localhost:5001/api/Posts/activity-feed \
  --cookie cookies.txt
```

---

**Last Updated:** 2026-02-17  
**Version:** 1.0.0-prototype
