namespace ImageProcessor.Api.Dtos
{
    public sealed record UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
    }
}
