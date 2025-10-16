using Microsoft.AspNetCore.Identity;

namespace TravelHub.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        // Pola własne aplikacji
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Przykładowe pole - kiedy konto zostało utworzone
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
