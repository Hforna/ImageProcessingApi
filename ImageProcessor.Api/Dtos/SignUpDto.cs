namespace ImageProcessor.Api.Dtos
{
    public sealed record SignUpDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
