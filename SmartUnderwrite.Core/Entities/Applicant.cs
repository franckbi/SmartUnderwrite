using SmartUnderwrite.Core.ValueObjects;

namespace SmartUnderwrite.Core.Entities;

public class Applicant
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string SsnHash { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}