using Microsoft.AspNetCore.Identity;

namespace RedMangoApi.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
    }
}
