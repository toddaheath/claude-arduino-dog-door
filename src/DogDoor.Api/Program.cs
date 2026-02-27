using System.Text;
using Asp.Versioning;
using DogDoor.Api.Data;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog — compact JSON logging driven from appsettings.json
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

// Database
builder.Services.AddDbContext<DogDoorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// JWT Authentication
var jwtSecretKey = builder.Configuration["JWT:SecretKey"];
if (!string.IsNullOrEmpty(jwtSecretKey))
{
    // Fail fast if someone deploys with the default placeholder secret
    if (jwtSecretKey.Contains("change-me", StringComparison.OrdinalIgnoreCase) && !builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "JWT:SecretKey contains the default placeholder value. " +
            "Set a strong, unique secret key via environment variable or Helm --set before deploying.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                ValidateIssuer = !string.IsNullOrEmpty(builder.Configuration["JWT:Issuer"]),
                ValidIssuer = builder.Configuration["JWT:Issuer"],
                ValidateAudience = !string.IsNullOrEmpty(builder.Configuration["JWT:Audience"]),
                ValidAudience = builder.Configuration["JWT:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
}
else if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "JWT:SecretKey is required in non-development environments. " +
        "Set it via JWT__SecretKey environment variable or configuration.");
}
else
{
    // Fallback for development/test environments without JWT config
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();
}

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAnimalService, AnimalService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IAnimalRecognitionService, AnimalRecognitionService>();
builder.Services.AddScoped<IDoorService, DoorService>();
builder.Services.AddScoped<ISmsService, TwilioSmsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc()
  .AddApiExplorer(options =>
  {
      options.GroupNameFormat = "'v'VVV";
      options.SubstituteApiVersionInUrl = true;
  });

// Health checks
builder.Services.AddHealthChecks();

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins == null || corsOrigins.Length == 0)
{
    corsOrigins = new[] { "http://localhost:5173", "http://localhost:3000" };
}
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/healthz").AllowAnonymous();
app.MapControllers();

// Ensure uploads directory exists (photos served via authenticated PhotosController.GetFile)
var uploadsPath = Path.Combine(app.Environment.ContentRootPath,
    builder.Configuration.GetValue<string>("PhotoStorage:BasePath") ?? "uploads");
Directory.CreateDirectory(uploadsPath);
Directory.CreateDirectory(Path.Combine(uploadsPath, "events"));
Directory.CreateDirectory(Path.Combine(uploadsPath, "approach"));

// Run migrations (or EnsureCreated for non-relational, e.g., in-memory test DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DogDoorDbContext>();
    if (db.Database.IsRelational())
    {
        // If the DB was previously created via EnsureCreated it will have tables but no
        // __EFMigrationsHistory row. Create the history table and record InitialCreate as
        // already applied so MigrateAsync doesn't try to re-create existing tables.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId"    character varying(150) NOT NULL,
                "ProductVersion" character varying(32)  NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );
            """);

        // If Animals exists but history is empty, the DB was created by EnsureCreated
        // (pre-migration). Mark InitialCreate as applied so MigrateAsync skips it and
        // only runs AddMultiUserSupport, which uses IF NOT EXISTS SQL throughout.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '20260217200700_InitialCreate', '9.0.13'
            WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory")
              AND EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'Animals'
              );
            """);

        // If Users also already exists (e.g. a dev DB that ran EnsureCreated after the
        // multi-user models were added), mark AddMultiUserSupport applied too.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '20260218000000_AddMultiUserSupport', '9.0.13'
            WHERE NOT EXISTS (
                SELECT 1 FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = '20260218000000_AddMultiUserSupport'
              )
              AND EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'Users'
              );
            """);

        // If NotificationPreferences already exists (EnsureCreated after this model was added),
        // mark AddNotificationPreferences as applied so MigrateAsync skips it.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '20260220140432_AddNotificationPreferences', '9.0.13'
            WHERE NOT EXISTS (
                SELECT 1 FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = '20260220140432_AddNotificationPreferences'
              )
              AND EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'NotificationPreferences'
              );
            """);

        await db.Database.MigrateAsync();
    }
    else
        await db.Database.EnsureCreatedAsync();
}

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
