using Microsoft.AspNetCore.Identity;

namespace ImageProcessor.Api.Model
{
    public class User : IdentityUser<Guid>
    {
        public Guid UserIdentifier { get; set; } = Guid.NewGuid();
    }
}
