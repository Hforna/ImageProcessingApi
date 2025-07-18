﻿namespace ImageProcessor.Api.Dtos
{
    public sealed record SignInResponseDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
