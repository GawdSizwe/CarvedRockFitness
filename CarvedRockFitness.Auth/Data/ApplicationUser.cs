using Microsoft.AspNetCore.Identity;

namespace CarvedRockFitness.Auth.Data;

public class ApplicationUser : IdentityUser
{
    public required string FullName { get; set; }
    public bool IsPaidUser { get; set; }
}
