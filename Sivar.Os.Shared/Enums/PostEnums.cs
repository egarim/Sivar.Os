namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Types of posts that can be created in the activity stream
/// Different profile types have access to different post types
/// </summary>
public enum PostType
{
    /// <summary>
    /// General text post with optional media and location
    /// Available to: All profile types
    /// </summary>
    General = 1,
    
    /// <summary>
    /// Business location post (office, branch, store)
    /// Available to: Business profiles only
    /// </summary>
    BusinessLocation = 2,
    
    /// <summary>
    /// Product showcase post with pricing and availability
    /// Available to: Business profiles only
    /// </summary>
    Product = 3,
    
    /// <summary>
    /// Service offering post with pricing and description
    /// Available to: Business profiles only
    /// </summary>
    Service = 4,
    
/// <summary>
/// Event announcement post
/// Available to: Organization, Business profiles
/// </summary>
Event = 5,
    
/// <summary>
/// Job posting
/// Available to: Business, Organization profiles
/// </summary>
JobPosting = 6,

/// <summary>
/// Blog post - long-form content with rich text
/// Available to: All profile types (based on FeatureFlags)
/// </summary>
Blog = 7,

/// <summary>
/// Procedure/How-to guide post with structured steps and requirements
/// Available to: All profile types
/// Examples: Government procedures, business processes, tutorials, how-to guides
/// </summary>
Procedure = 8
}

/// <summary>
/// Types of reactions users can have on posts and comments
/// </summary>
public enum ReactionType
{
    Like = 1,
    Love = 2,
    Laugh = 3,
    Wow = 4,
    Sad = 5,
    Angry = 6,
    Care = 7
}

/// <summary>
/// Types of business locations
/// </summary>
public enum BusinessLocationType
{
    /// <summary>
    /// Main office or headquarters
    /// </summary>
    MainOffice = 1,
    
    /// <summary>
    /// Branch office that serves customers
    /// </summary>
    CustomerBranch = 2,
    
    /// <summary>
    /// Administrative office (no customer service)
    /// </summary>
    AdministrativeOffice = 3,
    
    /// <summary>
    /// Retail store or showroom
    /// </summary>
    RetailStore = 4,
    
    /// <summary>
    /// Warehouse or distribution center
    /// </summary>
    Warehouse = 5,
    
    /// <summary>
    /// Service center or repair facility
    /// </summary>
    ServiceCenter = 6
}

/// <summary>
/// Types of media attachments for posts
/// </summary>
public enum AttachmentType
{
    /// <summary>
    /// Image file (JPG, PNG, GIF, etc.)
    /// </summary>
    Image = 1,
    
    /// <summary>
    /// Video file (MP4, AVI, etc.)
    /// </summary>
    Video = 2,
    
    /// <summary>
    /// External link or URL
    /// </summary>
    Link = 3,
    
    /// <summary>
    /// Document file (PDF, DOC, etc.)
    /// </summary>
    Document = 4,
    
    /// <summary>
    /// Audio file (MP3, WAV, etc.)
    /// </summary>
    Audio = 5
}

/// <summary>
/// Currency types for pricing
/// </summary>
public enum Currency
{
    /// <summary>
    /// US Dollar
    /// </summary>
    USD = 1,
    
    /// <summary>
    /// El Salvador Colón (legacy)
    /// </summary>
    SVC = 2,
    
    /// <summary>
    /// Euro
    /// </summary>
    EUR = 3,
    
    /// <summary>
    /// Other currency (specify in description)
    /// </summary>
    Other = 99
}

/// <summary>
/// Days of the week for business hours
/// </summary>
public enum DayOfWeek
{
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}

/// <summary>
/// Product or service availability status
/// </summary>
public enum AvailabilityStatus
{
    /// <summary>
    /// Available for purchase/booking
    /// </summary>
    Available = 1,
    
    /// <summary>
    /// Temporarily out of stock/unavailable
    /// </summary>
    OutOfStock = 2,
    
    /// <summary>
    /// Discontinued or no longer offered
    /// </summary>
    Discontinued = 3,
    
    /// <summary>
    /// Coming soon
    /// </summary>
    ComingSoon = 4
}