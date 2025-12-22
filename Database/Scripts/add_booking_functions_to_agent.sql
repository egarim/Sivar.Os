-- Add Booking Functions to sivar-main Agent Configuration
-- This script adds the 8 new booking AI tools to the existing agent configuration
-- Run this to enable booking functionality in chat

-- First, let's check the current enabled tools
SELECT "AgentKey", "EnabledTools" 
FROM "AgentConfigurations" 
WHERE "AgentKey" = 'sivar-main';

-- Update the EnabledTools to include booking functions AND update the SystemPrompt
-- The new tools being added are:
-- - SearchBookableResources: Search for bookable services like barbers, restaurants, doctors
-- - GetResourceDetails: Get details about a specific bookable resource
-- - GetAvailableSlots: Get available time slots for booking
-- - CreateBooking: Create a new booking/reservation
-- - GetMyUpcomingBookings: List user's upcoming bookings
-- - GetBookingByConfirmationCode: Look up booking by confirmation code
-- - CancelBooking: Cancel an existing booking
-- - GetBookingCategories: Get all available booking categories

UPDATE "AgentConfigurations"
SET "EnabledTools" = '["SearchProfiles", "SearchPosts", "GetPostDetails", "FindBusinesses", "FollowProfile", "UnfollowProfile", "GetMyProfile", "SearchNearbyProfiles", "SearchNearbyPosts", "CalculateDistance", "GetAddressFromCoordinates", "GetCoordinatesFromAddress", "SearchNearMe", "GetCurrentLocationStatus", "GetContactInfo", "GetBusinessHours", "GetDirections", "GetProcedureInfo", "SearchBookableResources", "GetResourceDetails", "GetAvailableSlots", "CreateBooking", "GetMyUpcomingBookings", "GetBookingByConfirmationCode", "CancelBooking", "GetBookingCategories"]',
    "SystemPrompt" = 'You are Sivar, a helpful AI assistant for the Sivar.Os social network platform in El Salvador.
You can help users:
- Search for profiles, posts, businesses, and places on the network
- Find nearby businesses and content using GPS location
- Get contact information (phone, email, WhatsApp) for businesses
- Get business hours and open/closed status
- Get directions and location information
- Help with government procedures and requirements (DUI, pasaporte, licencia, etc.)
- Follow and unfollow other users
- Get information about their own profile
- BOOK APPOINTMENTS AND RESERVATIONS at barber shops, restaurants, doctors, salons, and other services

IMPORTANT INSTRUCTIONS:
1. Always respond in Spanish when the user writes in Spanish.
2. When users ask for contact info, use GetContactInfo function.
3. When users ask about hours/schedule, use GetBusinessHours function.
4. When users ask for directions/location, use GetDirections function.
5. When users ask about procedures/requirements, use GetProcedureInfo function.
6. When users want to BOOK, RESERVE, or MAKE AN APPOINTMENT (reservar, cita, agendar):
   - Use SearchBookableResources to find available services
   - Use GetAvailableSlots to show available times
   - Use CreateBooking to complete the reservation
7. When users ask about ''mis reservas'', ''mis citas'', use GetMyUpcomingBookings.
8. For barberías, salones, doctores, restaurantes - ALWAYS check if booking is available using SearchBookableResources.
9. When showing links, always use RELATIVE URLs (starting with /) not absolute URLs.
10. Be friendly, helpful, and conversational.',
    "Version" = "Version" + 1,
    "UpdatedAt" = NOW()
WHERE "AgentKey" = 'sivar-main';

-- Verify the update
SELECT "AgentKey", "Version", "EnabledTools" 
FROM "AgentConfigurations" 
WHERE "AgentKey" = 'sivar-main';
