using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using SkillShareBackend.Data;
using SkillShareBackend.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

// Sentry se activa SOLO si hay un DSN configurado (config o variable de entorno).
// Si no hay DSN, la app arranca igual (el monitoreo nunca debe tumbar el servicio).
var sentryDsn = builder.Configuration["Sentry:Dsn"]
                ?? Environment.GetEnvironmentVariable("SENTRY_DSN");

if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.TracesSampleRate = 0.2;
        options.Environment = builder.Environment.EnvironmentName;
    });
    Console.WriteLine("✅ Sentry activado.");
}
else
{
    Console.WriteLine("⚠️ Sentry deshabilitado: no se encontró Sentry:Dsn ni SENTRY_DSN.");
}

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 30))));

builder.Environment.WebRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SkillShare API",
        Version = "v1",
        Description = "API del backend de SkillShare — monitoreo (Sentry, /health) y experimentos (EC-01 notificaciones, EC-02 y EC-05 recomendaciones)."
    });

    // Incluye los comentarios /// <summary> como descripción de cada endpoint.
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// Servicios registrados
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGroupManagementService, GroupManagementService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddSingleton<ChatWebSocketHandler>();
builder.Services.AddScoped<IFirebaseStorageService, FirebaseStorageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

/*
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));
*/

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            NameClaimType = ClaimTypes.NameIdentifier
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"🔴 JWT Authentication Failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"✅ JWT Token Validated for: {context.Principal.Identity.Name}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddHttpClient();
builder.Services.AddAuthorization();

// Initialize FirebaseApp for FCM
var firebaseConfigJson = builder.Configuration["GOOGLE_APPLICATION_CREDENTIALS_JSON"] 
                         ?? builder.Configuration["Firebase:Config"];

try
{
    if (FirebaseApp.DefaultInstance == null)
    {
        if (!string.IsNullOrEmpty(firebaseConfigJson))
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(firebaseConfigJson)
            });
        }
        else
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.GetApplicationDefault()
            });
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ FirebaseApp initialization skipped/failed: {ex.Message}");
}

var app = builder.Build();

// Crear directorios necesarios para almacenamiento de archivos
var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
    Console.WriteLine($"✅ Created wwwroot directory: {wwwrootPath}");
}

// Directorios para diferentes tipos de archivos
var directoriesToCreate = new[]
{
    Path.Combine(wwwrootPath, "uploads", "images"),
    Path.Combine(wwwrootPath, "uploads", "audio"),
    Path.Combine(wwwrootPath, "uploads", "files"),
    Path.Combine(wwwrootPath, "uploads", "documents")
};

foreach (var dir in directoriesToCreate)
    if (!Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
        Console.WriteLine($"✅ Created directory: {dir}");
    }

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
//app.UseHttpsRedirection();
app.UseStaticFiles(); // Importante para servir archivos estáticos

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseWebSockets();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "uploads")),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true
});

// Endpoint de prueba para verificar directorios
app.MapGet("/api/test-static-files", () =>
{
    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
    var imageDir = Path.Combine(uploadsDir, "images");
    var audioDir = Path.Combine(uploadsDir, "audio");
    var filesDir = Path.Combine(uploadsDir, "files");

    return new
    {
        uploadsDirectoryExists = Directory.Exists(uploadsDir),
        imageDirectoryExists = Directory.Exists(imageDir),
        audioDirectoryExists = Directory.Exists(audioDir),
        filesDirectoryExists = Directory.Exists(filesDir),
        currentDirectory = Directory.GetCurrentDirectory(),
        wwwrootPath = app.Environment.WebRootPath,
        totalImagesFiles = Directory.Exists(imageDir) ? Directory.GetFiles(imageDir).Length : 0,
        totalAudioFiles = Directory.Exists(audioDir) ? Directory.GetFiles(audioDir).Length : 0,
        totalOtherFiles = Directory.Exists(filesDir) ? Directory.GetFiles(filesDir).Length : 0
    };
});

// WebSocket para llamadas
app.Map("/ws/call/{callId}", async (HttpContext context, string callId) =>
{
    var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
    await handler.HandleCallWebSocket(context, callId);
});


app.Map("/ws/chat/{groupId}", async (HttpContext context, int groupId) =>
{
    var handler = context.RequestServices.GetRequiredService<ChatWebSocketHandler>();
    await handler.HandleChatWebSocket(context, groupId);
});

app.MapGet("/api/debug/uploads", () =>
{
    var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    var uploadsPath = Path.Combine(wwwrootPath, "uploads");
    var imagesPath = Path.Combine(uploadsPath, "images");
    var audioPath = Path.Combine(uploadsPath, "audio");
    var filesPath = Path.Combine(uploadsPath, "files");

    return new
    {
        wwwrootExists = Directory.Exists(wwwrootPath),
        uploadsExists = Directory.Exists(uploadsPath),
        imagesExists = Directory.Exists(imagesPath),
        audioExists = Directory.Exists(audioPath),
        filesExists = Directory.Exists(filesPath),
        currentDirectory = Directory.GetCurrentDirectory(),
        wwwrootPath,
        uploadsPath
    };
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/test-error", () =>
{
    throw new Exception("Prueba crítica de Sentry para demostración");
});

app.MapGet("/", () => "🚀 SkillShare Flutter Backend is Running!");

app.Run();