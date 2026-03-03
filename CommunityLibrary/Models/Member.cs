using System.ComponentModel.DataAnnotations;

namespace CommunityLibrary.Models;

public class Member
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    // Navigation property
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}