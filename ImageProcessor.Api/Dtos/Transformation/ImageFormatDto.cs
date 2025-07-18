﻿using ImageProcessor.Api.Enums;

namespace ImageProcessor.Api.Dtos.Transformation
{
    public sealed record ImageFormatDto
    {
        public ImageTypesEnum FormatType { get; set; }
    }
}
