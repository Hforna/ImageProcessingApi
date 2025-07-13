# Image Processing API with Azure Storage

![Azure Blob Storage](https://img.shields.io/badge/Azure%20Blob%20Storage-0089D6?logo=microsoft-azure&logoColor=white)
![Image Processing](https://img.shields.io/badge/Image%20Processing-FF6F61?logo=opencv&logoColor=white)

A robust API for uploading images to Azure Blob Storage and performing various image transformations.

## ✨ Features

- **Cloud Storage**
  - Secure upload/download to Azure Blob Storage
  - Automatic file organization
  - Metadata management

- **Image Transformations**
  - 🔄 Rotate (90°, 180°, 270°)
  - ↔️ Flip/Mirror (horizontal, vertical)
  - 📏 Resize (with aspect ratio preservation)
  - 🎨 Filters (grayscale, sepia, blur, sharpen)
  - ✂️ Crop (with customizable regions)
  - 🌈 Adjustments (brightness, contrast, saturation)

- **Advanced Features**
  - Batch processing
  - Format conversion (JPG, PNG, WEBP, etc.)
  - Transformation pipelines
  - Thumbnail generation

## 🚀 Quick Start

### Prerequisites
- Azure account with Blob Storage
- .NET 6.0+ SDK
- Azure Storage SDK

### Installation
```bash
git clone https://github.com/yourusername/image-processing-api.git
cd image-processing-api
dotnet restore
