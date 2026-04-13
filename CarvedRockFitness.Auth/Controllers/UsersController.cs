using CarvedRockFitness.Auth.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace CarvedRockFitness.Auth.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class UsersController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMembers(CancellationToken ct)
    {
        var users = await userManager.Users.ToListAsync(ct);

        var result = new List<object>();
        foreach (var u in users)
        {
            var roles  = await userManager.GetRolesAsync(u);
            var role   = roles.Contains("Admin") ? "Admin"
                       : roles.Contains("PaidUser") ? "PaidUser"
                       : "FreeUser";
            var locked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow;

            result.Add(new
            {
                Id         = u.Id,
                Name       = u.FullName,
                Email      = u.Email ?? "",
                Role       = role,
                IsPaidUser = u.IsPaidUser,
                IsBanned   = locked
            });
        }

        return Ok(result.OrderBy(x => ((dynamic)x).Name));
    }

    [HttpPatch("{id}/ban")]
    public async Task<IActionResult> BanUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            await userManager.SetLockoutEndDateAsync(user, null);
        }
        else
        {
            await userManager.SetLockoutEnabledAsync(user, true);
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        await userManager.DeleteAsync(user);
        return NoContent();
    }
}
