using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using TimeTracker.API.Data;
using TimeTracker.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
builder.Services.AddDbContext<TimeTrackerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// AWS S3 configuration
builder.Services.AddSingleton<IAmazonS3>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var awsOptions = new Amazon.S3.AmazonS3Config
    {
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(config["AWS:Region"] ?? "us-east-1")
    };

    return new Amazon.S3.AmazonS3Client(
        config["AWS:AccessKey"],
        config["AWS:SecretKey"],
        awsOptions
    );
});

builder.Services.AddScoped<IS3Service, S3Service>();

// CORS configuration for client communication
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowClient");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TimeTrackerDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();
