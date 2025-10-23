using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.DTOs.Metadata;

/// <summary>
/// Metadata DTO for Organization Profile type
/// </summary>
public class OrganizationProfileMetadataDto
{
    /// <summary>
    /// Organization type (NGO, Non-profit, Government, etc.)
    /// </summary>
    [Required(ErrorMessage = "Organization type is required")]
    [StringLength(100, ErrorMessage = "Organization type cannot exceed 100 characters")]
    public string OrganizationType { get; set; } = string.Empty;

    /// <summary>
    /// Organization mission statement
    /// </summary>
    [StringLength(1000, ErrorMessage = "Mission cannot exceed 1000 characters")]
    public string Mission { get; set; } = string.Empty;

    /// <summary>
    /// Organization vision statement
    /// </summary>
    [StringLength(1000, ErrorMessage = "Vision cannot exceed 1000 characters")]
    public string Vision { get; set; } = string.Empty;

    /// <summary>
    /// Organization values
    /// </summary>
    public List<string> Values { get; set; } = new();

    /// <summary>
    /// Programs or initiatives run by the organization
    /// </summary>
    public List<OrganizationProgram> Programs { get; set; } = new();

    /// <summary>
    /// Key leadership team members
    /// </summary>
    public List<TeamMember> Leadership { get; set; } = new();

    /// <summary>
    /// Organization registration number
    /// </summary>
    [StringLength(100, ErrorMessage = "Registration number cannot exceed 100 characters")]
    public string RegistrationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Tax-exempt status information
    /// </summary>
    [StringLength(100, ErrorMessage = "Tax status cannot exceed 100 characters")]
    public string TaxStatus { get; set; } = string.Empty;

    /// <summary>
    /// Year the organization was founded
    /// </summary>
    public int? YearFounded { get; set; }

    /// <summary>
    /// Number of employees/volunteers
    /// </summary>
    [StringLength(50, ErrorMessage = "Team size cannot exceed 50 characters")]
    public string TeamSize { get; set; } = string.Empty;

    /// <summary>
    /// Annual budget range
    /// </summary>
    [StringLength(50, ErrorMessage = "Budget range cannot exceed 50 characters")]
    public string BudgetRange { get; set; } = string.Empty;

    /// <summary>
    /// Geographic areas served
    /// </summary>
    public List<string> ServiceAreas { get; set; } = new();

    /// <summary>
    /// Partnerships with other organizations
    /// </summary>
    public List<string> Partners { get; set; } = new();

    /// <summary>
    /// Awards and recognitions received
    /// </summary>
    public List<OrganizationAward> Awards { get; set; } = new();

    /// <summary>
    /// Upcoming events hosted by the organization
    /// </summary>
    public List<OrganizationEvent> UpcomingEvents { get; set; } = new();

    /// <summary>
    /// Whether the organization accepts donations
    /// </summary>
    public bool AcceptsDonations { get; set; } = false;

    /// <summary>
    /// Whether the organization needs volunteers
    /// </summary>
    public bool NeedsVolunteers { get; set; } = false;

    /// <summary>
    /// Impact metrics and statistics
    /// </summary>
    public OrganizationImpact Impact { get; set; } = new();
}

/// <summary>
/// Program or initiative run by an organization
/// </summary>
public class OrganizationProgram
{
    /// <summary>
    /// Program name
    /// </summary>
    [Required]
    [StringLength(200, ErrorMessage = "Program name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Program description
    /// </summary>
    [StringLength(1000, ErrorMessage = "Program description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Target beneficiaries
    /// </summary>
    [StringLength(200, ErrorMessage = "Target audience cannot exceed 200 characters")]
    public string TargetAudience { get; set; } = string.Empty;

    /// <summary>
    /// Program start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Program end date (if applicable)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether the program is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Program budget
    /// </summary>
    [StringLength(50, ErrorMessage = "Budget cannot exceed 50 characters")]
    public string Budget { get; set; } = string.Empty;
}

/// <summary>
/// Team member information
/// </summary>
public class TeamMember
{
    /// <summary>
    /// Member name
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Member position/title
    /// </summary>
    [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters")]
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Member biography
    /// </summary>
    [StringLength(500, ErrorMessage = "Biography cannot exceed 500 characters")]
    public string Biography { get; set; } = string.Empty;

    /// <summary>
    /// Member photo URL
    /// </summary>
    [Url(ErrorMessage = "Photo must be a valid URL")]
    public string PhotoUrl { get; set; } = string.Empty;

    /// <summary>
    /// LinkedIn profile URL
    /// </summary>
    [Url(ErrorMessage = "LinkedIn must be a valid URL")]
    public string LinkedInUrl { get; set; } = string.Empty;
}

/// <summary>
/// Organization award or recognition
/// </summary>
public class OrganizationAward
{
    /// <summary>
    /// Award name
    /// </summary>
    [Required]
    [StringLength(200, ErrorMessage = "Award name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Organization that granted the award
    /// </summary>
    [StringLength(200, ErrorMessage = "Issuer cannot exceed 200 characters")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Date the award was received
    /// </summary>
    public DateTime? DateReceived { get; set; }

    /// <summary>
    /// Award description or criteria
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Organization event information
/// </summary>
public class OrganizationEvent
{
    /// <summary>
    /// Event name
    /// </summary>
    [Required]
    [StringLength(200, ErrorMessage = "Event name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Event description
    /// </summary>
    [StringLength(1000, ErrorMessage = "Event description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Event date and time
    /// </summary>
    public DateTime? DateTime { get; set; }

    /// <summary>
    /// Event location
    /// </summary>
    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Whether registration is required
    /// </summary>
    public bool RequiresRegistration { get; set; } = false;

    /// <summary>
    /// Registration URL
    /// </summary>
    [Url(ErrorMessage = "Registration URL must be a valid URL")]
    public string RegistrationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Event capacity
    /// </summary>
    public int? Capacity { get; set; }
}

/// <summary>
/// Organization impact metrics
/// </summary>
public class OrganizationImpact
{
    /// <summary>
    /// Number of people served/helped
    /// </summary>
    public int? PeopleServed { get; set; }

    /// <summary>
    /// Number of projects completed
    /// </summary>
    public int? ProjectsCompleted { get; set; }

    /// <summary>
    /// Funds raised (as string to handle currencies)
    /// </summary>
    [StringLength(50, ErrorMessage = "Funds raised cannot exceed 50 characters")]
    public string FundsRaised { get; set; } = string.Empty;

    /// <summary>
    /// Volunteer hours contributed
    /// </summary>
    public int? VolunteerHours { get; set; }

    /// <summary>
    /// Additional impact metrics
    /// </summary>
    public Dictionary<string, string> CustomMetrics { get; set; } = new();
}