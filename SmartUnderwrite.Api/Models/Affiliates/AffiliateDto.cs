namespace SmartUnderwrite.Api.Models.Affiliates;

public class AffiliateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int UserCount { get; set; }
    public int ApplicationCount { get; set; }
}

public class CreateAffiliateRequest
{
    public string Name { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
}

public class UpdateAffiliateRequest
{
    public string Name { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AffiliateUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class AssignUserToAffiliateRequest
{
    public int UserId { get; set; }
}