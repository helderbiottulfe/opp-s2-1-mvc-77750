using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityLibrary.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RolesController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;
    
    public RolesController(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }
    
    // GET: Admin/Roles
    public async Task<IActionResult> Index()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return View(roles);
    }
    
    // POST: Admin/Roles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string roleName)
    {
        if (!string.IsNullOrWhiteSpace(roleName))
        {
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName.Trim()));
                TempData["Success"] = $"Role '{roleName}' created successfully.";
            }
            else
            {
                TempData["Error"] = $"Role '{roleName}' already exists.";
            }
        }
        
        return RedirectToAction(nameof(Index));
    }
    
    // POST: Admin/Roles/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        
        if (role != null)
        {
            // Prevent deleting Admin role
            if (role.Name == "Admin")
            {
                TempData["Error"] = "Cannot delete the Admin role.";
                return RedirectToAction(nameof(Index));
            }
            
            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Role '{role.Name}' deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Error deleting role.";
            }
        }
        
        return RedirectToAction(nameof(Index));
    }
}