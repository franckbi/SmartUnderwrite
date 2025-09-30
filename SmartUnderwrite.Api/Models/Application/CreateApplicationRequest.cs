using System.ComponentModel.DataAnnotations;

namespace SmartUnderwrite.Api.Models.Application;

public class CreateApplicationRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(11, MinimumLength = 9)]
    public string Ssn { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public AddressDto Address { get; set; } = new();

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ProductType { get; set; } = string.Empty;

    [Required]
    [Range(1000, 1000000)]
    public decimal Amount { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal IncomeMonthly { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string EmploymentType { get; set; } = string.Empty;

    [Range(300, 850)]
    public int? CreditScore { get; set; }
}

public class AddressDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(10, MinimumLength = 5)]
    public string ZipCode { get; set; } = string.Empty;
}