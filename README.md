# Image Processing API with Azure Storage & RabbitMQ

A robust API for uploading images to Azure Blob Storage, processing them in background with RabbitMQ, and delivering results via callback URLs.

![Azure Blob Storage + RabbitMQ](https://img.shields.io/badge/Azure%20Blob-RabbitMQ-purple) 
![.NET 6+](https://img.shields.io/badge/.NET-6+-blueviolet)

## ✨ Features

### Cloud Storage
- Secure upload/download to Azure Blob Storage
- Automatic file organization with metadata management
- SAS URL generation for temporary access

### Image Transformations
- **Rotation**: 90°, 180°, 270°
- **Flip/Mirror**: Horizontal, vertical
- **Resize**: With aspect ratio preservation
- **Filters**: Grayscale, sepia, blur, sharpen
- **Crop**: Customizable regions

### Advanced Processing
- **Background Processing**: RabbitMQ queue integration
- **Callback Notifications**: HTTP POST to your endpoint when processing completes
- **Batch Processing**: Process multiple images in one request
- **Format Conversion**: JPG, PNG, WEBP, GIF

## 🚀 Quick Start

### Prerequisites
- Azure account with Blob Storage configured
- RabbitMQ instance (local or cloud)
- .NET 6.0+ SDK
- Azure Storage SDK
