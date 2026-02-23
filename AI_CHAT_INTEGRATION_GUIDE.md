# 🤖 AI Chat Integration - Complete Guide

**Status:** ✅ **100% FUNCTIONAL**  
**Date:** 2026-02-17  
**System:** Sivar.Os AI Assistant + Resource Booking System

---

## 🎉 **WHAT'S ALREADY WORKING**

Users can **book appointments through natural language chat**! No forms, no clicking—just conversation! ✨

### **Example Conversations:**

```
👤 User: "I need a haircut tomorrow"
🤖 AI: *Searches for barbers*
    "I found 3 barbershops near you. Which one would you like?"
    *Shows cards with photos, ratings, prices*

👤 User: "The first one looks good. What times are available?"
🤖 AI: *Checks availability*
    "They have slots available:
    • 10:00 AM
    • 2:30 PM
    • 4:00 PM"

👤 User: "Book me at 2:30 PM"
🤖 AI: *Creates booking*
    "✅ Booked! Your confirmation code is ABC-1234"
```

**For Photo Studio:**
```
👤 "I need a wedding photographer for next Saturday"
🤖 "Found Studio Fotográfico El Salvador! They offer:
    • Wedding Photography - $800 (8 hours)
    • Quinceañera Photography - $450 (4 hours)
    • Professional Portraits - $150 (1 hour)
    
    Which package interests you?"

👤 "The wedding package. What dates are free?"
🤖 *Shows available slots*

👤 "Book Saturday at 10 AM"
🤖 "✅ Wedding photography booked! Code: XYZ-789"
```

---

## 🧠 **HOW IT WORKS**

### **Architecture:**

```
User Message
    ↓
DxAIChat (DevExpress UI)
    ↓
ChatService (Microsoft.Extensions.AI + Agents Framework)
    ↓
Intent Classifier (routes to appropriate handler)
    ↓
BookingFunctions (AI-callable tools) ← YOU ARE HERE
    ↓
ResourceBookingService (business logic)
    ↓
Database + API
    ↓
Structured Results → Rendered as Cards/List/Grid/Map
```

### **Key Components:**

1. **DxAIChat** - DevExpress Blazor chat UI with streaming
2. **ChatService** - Main orchestrator using Microsoft AI framework
3. **BookingFunctions** - 8 AI-callable functions for bookings
4. **AgentFactory** - Dynamic agent loading
5. **IntentClassifier** - Smart query routing
6. **SearchResultService** - Formats results for UI cards

---

## 🛠️ **AI FUNCTIONS (Already Implemented)**

### **1. SearchBookableResources**
```csharp
[Description("Search for services that accept APPOINTMENTS...")]
public async Task<string> SearchBookableResources(
    string query,           // "haircut", "restaurant", "dentist"
    string? category,       // Barber, Restaurant, Doctor, etc.
    int maxResults = 5)
```

**What it does:**
- Searches bookable resources by name/type/category
- Returns structured results (photos, ratings, prices)
- Populates `LastSearchResults` for card rendering
- **Memory Guard** - Tracks valid resource IDs to prevent AI hallucination

**User queries that trigger this:**
- "I need a haircut"
- "Find restaurants for dinner"
- "Looking for a dentist"
- "Book a photographer for my wedding"

---

### **2. GetResourceDetails**
```csharp
[Description("Get detailed information about a specific bookable resource...")]
public async Task<string> GetResourceDetails(Guid resourceId)
```

**What it does:**
- Gets full details: services, hours, pricing, availability
- Shows weekly schedule
- Lists all services offered

**User queries:**
- "Tell me more about this studio"
- "What services do they offer?"
- "What are their hours?"

---

### **3. GetAvailableSlots**
```csharp
[Description("Check available time slots for booking...")]
public async Task<string> GetAvailableSlots(
    Guid resourceId,
    string? date,           // "2026-02-20" or "tomorrow"
    Guid? serviceId,
    int daysAhead = 1,
    string timeZone = "America/El_Salvador")
```

**What it does:**
- Checks availability for specific dates
- Can check multiple days ahead (up to 7)
- Respects timezone
- Shows duration, price, capacity

**User queries:**
- "What times are available tomorrow?"
- "Are they open this Saturday?"
- "Show me the next 3 days"

---

### **4. CreateBooking** ⚠️ **Requires Confirmation**
```csharp
[Description("Create a booking or reservation...")]
public async Task<string> CreateBooking(
    Guid resourceId,        // From search results
    string startTime,       // "2026-02-20 14:30"
    Guid? serviceId,
    string? notes,
    int guestCount = 1,
    string timeZone = "UTC")
```

**What it does:**
- Creates the actual booking
- Generates confirmation code
- Validates time slot is still available
- Checks booking window and policies

**Safety:**
- AI MUST confirm with user before calling
- Validates resource ID against search results (Memory Guard)
- Requires user to be logged in

**User queries:**
- "Book me at 2:30 PM"
- "Reserve a table for 4 people at 7 PM"
- "I'll take the wedding package for Saturday"

---

### **5. GetMyUpcomingBookings**
```csharp
[Description("Get the user's upcoming reservations and appointments...")]
public async Task<string> GetMyUpcomingBookings()
```

**What it does:**
- Shows all upcoming bookings
- Sorted by date
- Includes confirmation codes, statuses

**User queries:**
- "What are my appointments?"
- "Show my bookings"
- "Do I have anything scheduled?"

---

### **6. GetBookingByConfirmationCode**
```csharp
[Description("Look up a booking using its confirmation code...")]
public async Task<string> GetBookingByConfirmationCode(string confirmationCode)
```

**What it does:**
- Looks up booking by code (e.g., "ABC-1234")
- Shows full details and status
- Indicates if cancellation is possible

**User queries:**
- "Check my booking ABC-1234"
- "What's the status of confirmation code XYZ-789?"

---

### **7. CancelBooking** ⚠️ **Requires Confirmation**
```csharp
[Description("Cancel an existing booking or reservation...")]
public async Task<string> CancelBooking(
    Guid bookingId,
    string? reason)
```

**What it does:**
- Cancels the booking
- Verifies ownership
- Checks cancellation window
- Records reason

**Safety:**
- AI MUST confirm with user before calling
- Validates status (can't cancel completed bookings)
- Respects cancellation policies

**User queries:**
- "Cancel my appointment"
- "I need to cancel my booking ABC-1234"

---

### **8. GetBookingCategories**
```csharp
[Description("Get the list of available booking categories...")]
public Task<string> GetBookingCategories()
```

**What it does:**
- Lists all available service categories
- With descriptions

**User queries:**
- "What can I book here?"
- "What types of services are available?"

---

## 🛡️ **SAFETY FEATURES**

### **Memory Guard (Anti-Hallucination)**
```csharp
private HashSet<Guid> _validResourceIds = new();
```

**Problem it solves:**
- AI models sometimes "hallucinate" IDs (make them up)
- This causes failed bookings with "resource not found"

**Solution:**
- Tracks every resource ID returned from searches
- Before booking, validates ID was in search results
- Returns helpful error: "Use one of these valid IDs: ..."

**Example:**
```
❌ AI tries to use: "dcfa7f06-1234-5678-9abc-def012345678" (made up)
✅ System: "That ID wasn't in search results. Use: dcfa7f06-b22c-4627..."
```

### **Structured Results System**
```csharp
public SearchResultsCollectionDto? LastSearchResults { get; private set; }
```

**What it does:**
- Captures search results as structured data
- Renders beautiful cards in the UI (not just text)
- Shows photos, ratings, maps, pricing
- Multiple display modes: Cards, List, Grid, Map

**User Experience:**
```
Instead of:
"I found Studio Fotográfico El Salvador at..."

User sees:
[Photo Card]
Studio Fotográfico El Salvador ⭐⭐⭐⭐⭐
Wedding Photography - $800
📍 0.5 km away
[Book Now] [View Details]
```

---

## 🌍 **LOCATION AWARENESS**

### **Location Context:**
```csharp
public void SetCurrentLocation(double? latitude, double? longitude)
```

**Features:**
- Uses browser geolocation
- Defaults to San Salvador (13.6929, -89.2182)
- Shows distances: "0.5 km away"
- Sorts results by proximity
- Map view shows user location + results

**User Experience:**
```
👤 "Find restaurants near me"
🤖 Shows 5 closest restaurants
    Each shows: "📍 0.3 km away"
    Map shows pins + your location
```

---

## 📊 **TOKEN ALLOWANCE SYSTEM**

**What it is:**
- Prevents API abuse
- Tracks AI usage per user
- Monthly/daily limits
- Resets automatically

**User sees:**
```
Tokens remaining: 8,500 / 10,000
Resets: 2026-03-01
```

**When limit exceeded:**
```
"Token allowance exceeded. Your allowance resets on March 1st."
```

---

## 🎨 **UI DISPLAY MODES**

Users can switch between 4 views:

### **1. Cards (Carousel)**
```
[← → ]
┌─────────────────┐
│   [Photo]       │
│ Business Name   │
│ ⭐⭐⭐⭐⭐ 4.8   │
│ $150 • 1 hour   │
│ [Book Now]      │
└─────────────────┘
```

### **2. List**
```
● Studio A - Photography • 0.5 km
● Studio B - Events • 1.2 km
● Studio C - Portraits • 2.0 km
```

### **3. Grid**
```
┌────┐ ┌────┐ ┌────┐
│ A  │ │ B  │ │ C  │
└────┘ └────┘ └────┘
```

### **4. Map**
```
   ╔═══════════╗
   ║ 📍 You    ║
   ║  • Studio ║
   ║  • Studio ║
   ╚═══════════╝
```

---

## 🔧 **FOR DEVELOPERS**

### **Adding a New Function:**

1. **Create function in BookingFunctions.cs:**
```csharp
[Description("What this function does")]
public async Task<string> MyNewFunction(
    [Description("What this parameter does")]
    string param1)
{
    try
    {
        // Your logic here
        return JsonSerializer.Serialize(new { success = true, data = result });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in MyNewFunction");
        return JsonSerializer.Serialize(new { success = false, error = ex.Message });
    }
}
```

2. **AI automatically discovers it!**
   - No registration needed
   - Microsoft.Extensions.AI finds all `[Description]` methods
   - AI decides when to call based on description

### **Testing in Chat:**

```
1. Start dev server: dotnet run
2. Navigate to: https://dev.sivar.lat/app/chat
3. Type: "I need a haircut tomorrow"
4. Watch AI call functions in logs
5. See results rendered as cards
```

---

## 📝 **CONVERSATION EXAMPLES**

### **Full Booking Flow:**

```
👤 "I need to get my hair cut"

🤖 *Calls: SearchBookableResources("haircut")*
    "I found 3 barber shops near you:
    
    [Card 1: Barbería Central]
    ⭐⭐⭐⭐⭐ 4.9 (127 reviews)
    Haircut - $15 • 30 min
    📍 0.3 km away
    
    [Card 2: Style Masters]
    ⭐⭐⭐⭐ 4.6 (89 reviews)
    Haircut - $20 • 45 min
    📍 0.8 km away
    
    [Card 3: Quick Cuts]
    ⭐⭐⭐⭐ 4.2 (45 reviews)
    Haircut - $12 • 20 min
    📍 1.5 km away
    
    Which one would you like?"

👤 "The first one looks good"

🤖 *Calls: GetResourceDetails(id)*
    "Great choice! Barbería Central is open:
    • Mon-Fri: 9 AM - 7 PM
    • Saturday: 9 AM - 6 PM
    • Sunday: Closed
    
    Services:
    • Haircut - $15 (30 min)
    • Haircut + Beard - $25 (45 min)
    • Shave - $10 (20 min)
    
    When would you like to book?"

👤 "Tomorrow at 2 PM"

🤖 *Calls: GetAvailableSlots(tomorrow)*
    "Available slots tomorrow:
    • 1:00 PM ✅
    • 1:30 PM ✅
    • 2:00 PM ✅
    • 2:30 PM ✅
    • 3:00 PM ✅
    
    Would you like me to book you at 2:00 PM?"

👤 "Yes please"

🤖 *Calls: CreateBooking(resourceId, "2026-02-18 14:00")*
    "✅ Booked!
    
    Confirmation Code: ABC-1234
    
    Barbería Central
    Tomorrow at 2:00 PM
    Haircut - $15 (30 min)
    
    You'll receive a reminder 1 hour before.
    See you there! 💈"

👤 "Thanks!"

🤖 "You're welcome! Need anything else?"
```

### **Checking Bookings:**

```
👤 "What appointments do I have?"

🤖 *Calls: GetMyUpcomingBookings()*
    "You have 2 upcoming bookings:
    
    1. Barbería Central
       Tomorrow at 2:00 PM
       Code: ABC-1234
       
    2. Studio Fotográfico
       Saturday at 10:00 AM
       Code: XYZ-789"
```

### **Cancellation:**

```
👤 "I need to cancel my haircut"

🤖 "I found your booking at Barbería Central tomorrow at 2:00 PM.
    Are you sure you want to cancel?"

👤 "Yes"

🤖 *Calls: CancelBooking(id)*
    "✅ Cancelled!
    
    Your appointment at Barbería Central has been cancelled.
    No charge. You can rebook anytime!"
```

---

## 🚀 **WHAT'S NEXT?**

### **Already Working:**
✅ Natural language booking  
✅ Search with photos & ratings  
✅ Availability checking  
✅ Booking creation & management  
✅ Confirmation codes  
✅ Cancellations  
✅ Memory guard (anti-hallucination)  
✅ Structured card rendering  
✅ Map view  
✅ Location awareness  

### **Future Enhancements:**

**WhatsApp Integration:**
```
👤 *Sends WhatsApp: "I need a haircut tomorrow"*
🤖 *Bot responds with options*
👤 *Books via WhatsApp*
```

**Payment Integration:**
```
👤 "Book and pay now"
🤖 *Creates booking + payment link*
✅ Paid confirmation
```

**Smart Recommendations:**
```
👤 "I need a haircut"
🤖 "Based on your previous bookings, you might like:
    • Barbería Central (you went there last month)
    • Style Masters (highly rated, similar price)"
```

**Multi-Language:**
```
👤 "Necesito cortarme el pelo"
🤖 "Encontré 3 barberías cerca..."
```

---

## 📊 **SYSTEM STATUS**

```
Backend API:        ✅ 100% Working
Database:           ✅ 100% Working
AI Functions:       ✅ 100% Working (8/8)
UI Components:      ✅ 100% Working
Chat Integration:   ✅ 100% Working
Memory Guard:       ✅ 100% Working
Structured Results: ✅ 100% Working
Location Context:   ✅ 100% Working

Status: PRODUCTION READY 🎉
```

---

## 🎯 **KEY INSIGHTS**

1. **No Forms Needed** - Users just talk naturally
2. **Visual Results** - Cards with photos, not just text
3. **Safety First** - Confirmation required for actions
4. **Smart Context** - Remembers search results
5. **Location Aware** - Shows nearest options
6. **Anti-Hallucination** - Validates IDs
7. **Multi-Display** - Cards, List, Grid, Map
8. **Token Tracking** - Usage limits

---

## 💡 **BEST PRACTICES**

### **For AI Prompts:**

✅ **DO:**
- Use natural language
- Be specific: "haircut tomorrow at 2 PM"
- Ask follow-ups: "What other times?"

❌ **DON'T:**
- Use resource IDs directly
- Try to "trick" the system
- Exceed token limits

### **For Developers:**

✅ **DO:**
- Add logging to functions
- Use Memory Guard pattern
- Return structured JSON
- Include helpful error messages

❌ **DON'T:**
- Generate IDs randomly
- Skip validation
- Return plain text (use JSON)
- Forget try-catch blocks

---

## 🔗 **USEFUL LINKS**

**Test URLs:**
```
Chat:       https://dev.sivar.lat/app/chat
Bookings:   https://dev.sivar.lat/app/bookings
Studio:     https://dev.sivar.lat/app/photo-studio
API Docs:   https://dev.sivar.lat/swagger
```

**Code Locations:**
```
AI Functions:   /Sivar.Os/Services/AgentFunctions/BookingFunctions.cs
Chat Service:   /Sivar.Os/Services/ChatService.cs
Chat UI:        /Sivar.Os.Client/Components/AIChat/SivarAIChat.razor
Chat Page:      /Sivar.Os.Client/Pages/Chat.razor
```

---

**Built with:**
- Microsoft.Extensions.AI
- Microsoft.Agents.AI  
- DevExpress AIIntegration.Blazor.Chat
- .NET 8 + PostgreSQL

**Status:** ✅ **FULLY FUNCTIONAL**  
**Last Updated:** 2026-02-17  
**Version:** 1.0

---

Ready to revolutionize how people book appointments! 🚀✨
