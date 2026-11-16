using Khotwa.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

using Khotwa.Data;
using Microsoft.EntityFrameworkCore;
using Khotwa.Services;


var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// Database Connection
builder.Services.AddDbContext<KhotwaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("KhotwaConnection")
    )
);

// Allow Angular to access the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CourseService>();


builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<LessonService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<EnrollmentService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<StudentProfileService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// JWT Authentication
string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT key is not configured");

byte[] keyBytes = Encoding.UTF8.GetBytes(jwtKey);

if (keyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "JWT key must be at least 32 bytes");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(keyBytes),

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role,

                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization();



var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AngularPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
