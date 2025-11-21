using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using System.Text;
using arroyoSeco.Infrastructure.Data;
using arroyoSeco.Infrastructure.Auth;
using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Infrastructure.Services;
using arroyoSeco.Application.Features.Alojamiento.Commands.Crear;
using arroyoSeco.Application.Features.Reservas.Commands.Crear;
using arroyoSeco.Application.Features.Reservas.Commands.CambiarEstado;
using arroyoSeco.Infrastructure.Storage;
using System.Text.Json.Serialization;
using System.Runtime.ExceptionServices;
using arroyoSeco.Services;

var builder = WebApplication.CreateBuilder(args);

// Tamaño máximo del body a nivel Kestrel (50 MB)
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 50_000_000;
});

builder.WebHost.UseKestrel(o =>
{
    o.ListenLocalhost(7190, lo => lo.UseHttps()); // solo puerto seguro
    // quita el puerto HTTP
});

builder.Services.AddHttpContextAccessor();

// Capturar excepciones globales (si algo revienta mostrar log)
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    Console.WriteLine("UNHANDLED: " + e.ExceptionObject);
};
TaskScheduler.UnobservedTaskException += (s, e) =>
{
    Console.WriteLine("UNOBSERVED: " + e.Exception);
    e.SetObserved();
};
AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
{
    Console.WriteLine("FIRST CHANCE: " + e.Exception.GetType().Name + " - " + e.Exception.Message);
};
builder.Services.AddHostedService<ShutdownLogger>();
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50_000_000;
    o.ValueLengthLimit = int.MaxValue;
    o.MemoryBufferThreshold = 1024 * 1024;
});

const string CorsPolicy = "FrontPolicy";
builder.Services.AddCors(p =>
{
    p.AddPolicy(CorsPolicy, policy =>
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36)),
        mySql => mySql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery) // <- va dentro del delegate del provider
    )
    .EnableSensitiveDataLogging()
);

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services
    .AddIdentityCore<IdentityUser>(opt =>
    {
        opt.User.RequireUniqueEmail = true;
        opt.Password.RequireDigit = false;
        opt.Password.RequireLowercase = false;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IFolioGenerator, FolioGenerator>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<CrearAlojamientoCommandHandler>();
builder.Services.AddScoped<CrearReservaCommandHandler>();
builder.Services.AddScoped<CambiarEstadoReservaCommandHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ArroyoSeco API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Bearer token"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new[] { "Bearer" } }
    });
});

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
var storage = builder.Configuration.GetSection("Storage").Get<StorageOptions>() ?? new StorageOptions();
if (string.IsNullOrWhiteSpace(storage.ComprobantesPath))
{
    storage.ComprobantesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "arroyoSeco", "comprobantes");
}
builder.Services.PostConfigure<StorageOptions>(o =>
{
    if (string.IsNullOrWhiteSpace(o.ComprobantesPath))
        o.ComprobantesPath = storage.ComprobantesPath;
});

var app = builder.Build();

// Middleware global de errores (evita cierre silencioso)
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine("GLOBAL EXCEPTION: " + ex);
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsync("Error interno");
    }
});

// Crear carpeta y servir archivos
var comprobantesPath = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value.ComprobantesPath;
Directory.CreateDirectory(comprobantesPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(comprobantesPath),
    RequestPath = "/comprobantes"
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

// Endpoint de salud para verificar que no se cayó
app.MapGet("/health", () => Results.Ok("OK"));

app.MapControllers();
app.Run();