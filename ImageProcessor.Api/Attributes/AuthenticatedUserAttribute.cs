using ImageProcessor.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessor.Api.Attributes
{
    public class AuthenticationUserAttribute : TypeFilterAttribute
    {
        public AuthenticationUserAttribute() : base(typeof(UserAuthenticated))
        {
        }
    }
}
