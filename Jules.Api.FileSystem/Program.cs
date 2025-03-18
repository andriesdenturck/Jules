using Jules.Access.Archive.Contracts;
using Jules.Access.Archive.Service;
using Jules.Access.Blob.Contracts;
using Jules.Access.Blob.Service;
using Jules.Api.FileSystem.Models;
using Jules.Engine.Parsing.Contracts;
using Jules.Engine.Parsing.Service;
using Jules.Manager.FileSystem.Contracts;
using Jules.Manager.FileSystem.Service;
using Jules.Util.Security;
using Jules.Util.Security.Contracts;
using Jules.Util.Security.Models;
using Jules.Util.Security.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ArchiveDbContext>(options => options.UseInMemoryDatabase("ArchiveDbContext"));
builder.Services.AddDbContext<BlobDbContext>(options => options.UseInMemoryDatabase("BlobDbContext"));
builder.Services.AddDbContext<SecurityDbContext>(options => options.UseInMemoryDatabase("SecurityDb"));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<SecurityDbContext>()
.AddDefaultTokenProviders(); // this registers UserManager and RoleManager

// Register your services
builder.Services.AddScoped<ISecurity, Security>();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();
builder.Services.AddScoped<IBlobAccess, BlobAccess>();
builder.Services.AddScoped<IArchiveAccess, ArchiveAccess>();
builder.Services.AddScoped<IParsingEngine, ParsingEngine>();
builder.Services.AddScoped<IFileSystemManager, FileSystemManager>();

builder.Services.AddScoped((provider) =>
{
    var options = provider.GetRequiredService<DbContextOptions<ArchiveDbContext>>();
    var userContext = provider.GetRequiredService<IUserContext>();
    return new ArchiveDbContext(options, userContext);
});

builder.Services.AddControllers()
       .AddJsonOptions(options =>
       {
           // Customize the JSON serializer settings
           options.JsonSerializerOptions.TypeInfoResolver = new AppJsonContext();
       });
builder.Services.AddControllers();
// Configure JWT Authentication
var key = "Le Tour du monde en quatre-vingts jours";  // Ideally, store this key in environment variables or a secret manager

var secretKey = builder.Configuration["JWTSettings:Key"];
var issuer = builder.Configuration["JWTSettings:Issuer"];
var audience = builder.Configuration["JWTSettings:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero, // Optional: Default is 5 minutes, set to 0 for stricter expiry validation
            ValidIssuer = issuer,
            ValidAudience = audience
        });

// Add authentication middleware
// Add authorization services
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    // Add JWT authentication scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.\r\n\r\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Add global security requirement
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Apply migrations and seed roles and users
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

    // Seed roles and users (Admin role and user)
    await IdentityDbSeeder.SeedAsync(userManager, roleManager);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FileSystem API V1");
        options.RoutePrefix = ""; // Serve Swagger UI at root
        options.EnableTryItOutByDefault();
    });

    //This makes authorization easier in Swagger
    app.Use(async (context, next) =>
    {
        var auth = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(auth) && !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Headers["Authorization"] = $"Bearer {auth}";
        }

        await next();
    });
}
app.UseRouting();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();   // Add authorization middleware

app.MapControllers();

app.Run();

[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(UserLogin))]
[JsonSerializable(typeof(FileRequest))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(Jules.Api.FileSystem.Models.Item[]))]
public partial class AppJsonContext : JsonSerializerContext
{ }

public partial class Program
{ }