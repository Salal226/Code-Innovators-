using Microsoft.AspNetCore.Identity;

namespace ITAssetManager.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}