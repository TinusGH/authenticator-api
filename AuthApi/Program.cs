using AuthApi.Data;
using AuthApi.Dtos;
using AuthApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                .Select(e => new { Field = e.Key, Message = e.Value!.Errors.First().ErrorMessage })
                .ToArray();

            return new BadRequestObjectResult(new
            {
                Message = "Validation failed",
                Errors = errors
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();

// JWT key from configuration
var key = builder.Configuration["JwtSettings:SecretKey"]

          ?? throw new InvalidOperationException("JWT SecretKey is missing");

// Configure JWT authentication BEFORE building the app

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated successfully for user: " + context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("JWT Validation Failed: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            Console.WriteLine("Token received: " + context.Token);
            return Task.CompletedTask;
        }
    };
});

//Swagger with JWT Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthApi", Version = "v1" });

    // Add JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Show example login data in Swagger - added this to make testing easier
    c.MapType<RegisterUserDto>(() => new OpenApiSchema
    {
        Type = "object",
        Properties =
        {
            ["email"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("tinus@example.com") },
            ["password"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("P@ssw0rd123") }
        }
    });

    c.MapType<LoginUserDto>(() => new OpenApiSchema
    {
        Type = "object",
        Properties =
        {
            ["email"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("tinus@example.com") },
            ["password"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("P@ssw0rd123") }
        }
    });

    c.MapType<LoginResponseDto>(() => new OpenApiSchema
    {
        Type = "object",
        Properties =
    {
        ["message"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("Login successful") },
        ["token"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("eyJhbGciOi...") },
        ["user"] = new OpenApiSchema
        {
            Type = "object",
            Properties =
            {
                ["id"] = new OpenApiSchema { Type = "integer", Example = new OpenApiInteger(1) },
                ["email"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("tinus@example.com") },
                ["createdAt"] = new OpenApiSchema { Type = "string", Format = "date-time", Example = new OpenApiString(DateTime.UtcNow.ToString("O")) }
            }
        }
    }
    });
});

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseInMemoryDatabase("TestDb"));
}
else
{
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// Register the UserService for dependency injection
builder.Services.AddScoped<IUserService, UserService>();

//builder.Services.AddOpenApi();
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1");
        c.RoutePrefix = "swagger"; // Swagger UI at root URL

        // Pre-authorize with a hardcoded token (for dev/testing only)
        c.ConfigObject.AdditionalItems["persistAuthorization"] = true;
    });

    // Redirect root to Swagger
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/" || context.Request.Path == "/index.html")
        {
            context.Response.Redirect("/swagger");
            return;
        }
        await next();
    });
}

if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
public partial class Program { }
