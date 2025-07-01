namespace ImageProcessor.Api.Dtos
{
    public sealed record SignInDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
