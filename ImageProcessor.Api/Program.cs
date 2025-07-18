using ImageProcessor.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ImageProcessor.Api.Model;
using ImageProcessor.Api.Repositories;
using ImageProcessor.Api.Services;
using AutoMapper;
using ImageProcessor.Api.RabbitMq.Producers;
using ImageProcessor.Api.RabbitMq.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRouting(d => d.LowercaseUrls = true);

var sqlConnection = builder.Configuration.GetConnectionString("sqlserver");

builder.Services
    .AddDbContext<DataContext>(d => d.UseSqlServer(sqlConnection));

builder.Services.AddIdentityCore<User>()
    .AddEntityFrameworkStores<DataContext>();

builder.Services.AddScoped(cfg =>
{
    var config = new MapperConfiguration(d => d.AddProfile(new MapperService()));

    return config.CreateMapper();
});

builder.Services.AddSingleton<ImageService>();

builder.Services.AddScoped<IStorageService, AzureBlobStorageService>(d => new AzureBlobStorageService(
    new Azure.Storage.Blobs.BlobServiceClient(builder.Configuration.GetValue<string>("services:azure:blobClient"))));

builder.Services.AddSingleton<IPasswordEncrypt, PasswordBcrypt>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

//rabbitmq
//consumers
builder.Services.AddHostedService<ResizeImageConsumer>();
builder.Services.AddHostedService<CropImageConsumer>();
builder.Services.AddHostedService<RotateImageConsumer>();
builder.Services.AddHostedService<WatermarkOnImageConsumer>();
builder.Services.AddHostedService<FilterOnImageConsumer>();

//producers
builder.Services.AddScoped<IProcessImageProducer, ProcessImageProducer>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient();

var signKey = builder.Configuration.GetValue<string>("services:jwt:signKey");
var expires = builder.Configuration.GetValue<int>("services:jwt:expiresAt");

builder.Services.AddScoped<ITokenService, TokenService>(d => new TokenService(signKey, 
    expires, 
    d.CreateScope().ServiceProvider.GetRequiredService<IUnitOfWork>(), d.CreateScope().ServiceProvider.GetRequiredService<IHttpContextAccessor>()));

builder.Services.AddTransient<IImageFilter, GrayscaleFilter>();
builder.Services.AddTransient<IImageFilter, SepiaFilter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
