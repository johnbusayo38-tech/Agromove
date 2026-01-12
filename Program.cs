


using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AgroMove.API.Data;
using AgroMove.API.Services;
using AgroMove.API.Hubs; 
using Microsoft.Extensions.FileProviders;
using System.Text.Json;
using System.Text.Json.Serialization;
using FirebaseAdmin; 
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================
// 1. FIREBASE ADMIN SDK INITIALIZATION
// ==========================================================
var firebaseServiceAccountPath = Path.Combine(builder.Environment.ContentRootPath, "firebase-service-account.json");

if (File.Exists(firebaseServiceAccountPath))
{
    if (FirebaseApp.DefaultInstance == null)
    {
        FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile(firebaseServiceAccountPath)
        });
        Console.WriteLine(">>> [SUCCESS] Firebase Admin SDK Initialized.");
    }
}
else
{
    Console.WriteLine(">>> [WARNING] firebase-service-account.json NOT FOUND. Push notifications will fail.");
}

// ==========================================================
// 2. DATABASE CONFIGURATION (PostgreSQL)
// ==========================================================
builder.Services.AddDbContext<AgroMoveDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================================
// 3. SERVICE REGISTRATION (Dependency Injection)
// ==========================================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// FIX: Add SignalR services to resolve OrdersController dependency
builder.Services.AddSignalR();

// ==========================================================
// 4. CONTROLLERS & JSON FORMATTING
// ==========================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Allow numbers to be read as strings automatically.
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString | 
                                                       JsonNumberHandling.WriteAsString;
    });

builder.Services.AddEndpointsApiExplorer();

// ==========================================================
// 5. SWAGGER WITH JWT SUPPORT
// ==========================================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "AgroMove API", 
        Version = "v1",
        Description = "AgroMove Backend with Firebase Push Notifications"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT: Bearer {token}"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ==========================================================
// 6. JWT AUTHENTICATION
// ==========================================================
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey)) throw new Exception("JWT Key is missing in appsettings.json.");
var key = Encoding.UTF8.GetBytes(jwtKey);

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
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    // Required for SignalR Authentication via Query String
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// ==========================================================
// 7. CORS CONFIGURATION
// ==========================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AgroMovePolicy", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",      
            "http://localhost:5173",      
            "http://localhost:8081",      
            "http://10.0.2.2:8081"        
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

// ==========================================================
// 8. STATIC FILES (Uploads)
// ==========================================================
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// ==========================================================
// 9. MIDDLEWARE PIPELINE
// ==========================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgroMove API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AgroMovePolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// FIX: Map the SignalR Hub endpoint
app.MapHub<NotificationHub>("/notificationHub");

app.Run();