using ImageProcessor.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessor.Api.Attributes
{
    public class AuthenticatedUserAttribute : TypeFilterAttribute
    {
        public AuthenticatedUserAttribute() : base(typeof(AuthenticatedUser))
        {
        }
    }
}
