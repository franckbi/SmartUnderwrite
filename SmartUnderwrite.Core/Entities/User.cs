using Microsoft.AspNetCore.Identity;

namespace SmartUnderwrite.Core.Entities;

public class User : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? AffiliateId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Affiliate? Affiliate { get; set; }
    public ICollection<Decision> Decisions { get; set; } = new List<Decision>();
}

public class Role : IdentityRole<int>
{
    public Role() { }
    public Role(string roleName) : base(roleName) { }
}