using ImageProcessor.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ImageProcessor.Api.Model;
using ImageProcessor.Api.Repositories;
using ImageProcessor.Api.Services;
using AutoMapper;

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

builder.Services.AddSingleton<IPasswordEncrypt, PasswordBcrypt>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

var signKey = builder.Configuration.GetValue<string>("services:jwt:signKey");
var expires = builder.Configuration.GetValue<int>("services:jwt:expiresAt");

builder.Services.AddScoped<ITokenService, TokenService>(d => new TokenService(signKey, 
    expires, 
    d.CreateScope().ServiceProvider.GetRequiredService<IUnitOfWork>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
