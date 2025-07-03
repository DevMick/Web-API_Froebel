using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using InstitutFroebel.API.Data;
using InstitutFroebel.API.Configuration;
using InstitutFroebel.API.Services;
using InstitutFroebel.API.Services.Interfaces;
using InstitutFroebel.API.Services.Implementations;
using InstitutFroebel.Core.Entities.Identity;
using InstitutFroebel.Core.Interfaces;
using InstitutFroebel.API.Mappings;
using FluentValidation;
using InstitutFroebel.API.Validators;
using InstitutFroebel.API.Middleware;
using Npgsql;
using InstitutFroebel.API.Data; // ← Important pour DbSeeder


var builder = WebApplication.CreateBuilder(args);

// Test de connexion base de données (temporaire)
TestDbConnection();

// FluentValidation Configuration
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

// Add services to the container.
builder.Services.AddControllers();

// AutoMapper Configuration
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Configuration JwtSettings
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

// Services Multi-Tenant
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();

// JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();

// Business Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IUserService, UserService>();

// PostgreSQL Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configuration du mot de passe
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Configuration de l'utilisateur
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Configuration du verrouillage
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Configuration de la confirmation d'email (désactivée pour le développement)
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication Configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero // Pas de tolérance sur l'expiration
    };

    // Configuration pour les événements JWT
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"JWT Token validated for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        }
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("AdminOrSuperAdmin", policy => policy.RequireRole("Admin", "SuperAdmin"));
    options.AddPolicy("ParentOrAdmin", policy => policy.RequireRole("Parent", "Admin", "SuperAdmin"));
    options.AddPolicy("TeacherOrAdmin", policy => policy.RequireRole("Teacher", "Admin", "SuperAdmin"));
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "https://localhost:3000",
            "http://localhost:3001",
            "https://localhost:3001"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
    // En développement, autorise tout (à retirer en prod)
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger Configuration avec JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Institut Froebel API",
        Version = "v1",
        Description = "API Multi-Tenant pour la gestion de l'Institut Froebel"
    });

    // Configuration JWT dans Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });

    // Support pour les headers tenant
    c.AddSecurityDefinition("School-Code", new OpenApiSecurityScheme
    {
        Description = "Code de l'école pour le multi-tenant. Example: \"X-School-Code: FROEBEL_ABJ\"",
        Name = "X-School-Code",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Institut Froebel API v1");
        options.RoutePrefix = "swagger";
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.UseHttpsRedirection();

// CORS
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowReactApp");
}

// Request Logging (en développement)
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<RequestLoggingMiddleware>();
}

// Authentication & Authorization (ordre important !)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed the database
await DbSeeder.SeedAsync(app.Services);

app.Run();

// Fonction de test de connexion
static void TestDbConnection()
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile("appsettings.Development.json", optional: true)
        .Build();

    var connectionString = configuration.GetConnectionString("DefaultConnection");

    Console.WriteLine($"Testing connection with: {connectionString}");

    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        Console.WriteLine("✅ Connexion PostgreSQL réussie !");

        // Test simple query
        using var command = new NpgsqlCommand("SELECT version();", connection);
        var result = command.ExecuteScalar();
        Console.WriteLine($"Version PostgreSQL: {result}");

        connection.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erreur de connexion : {ex.Message}");
        Console.WriteLine($"Détails: {ex}");
    }
}