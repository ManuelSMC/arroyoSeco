using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using arroyoSeco.Infrastructure.Data;
using arroyoSeco.Infrastructure.Auth;
using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Infrastructure.Services;
using arroyoSeco.Application.Features.Alojamiento.Commands.Crear;
using arroyoSeco.Application.Features.Reservas.Commands.Crear;
using arroyoSeco.Application.Features.Reservas.Commands.CambiarEstado;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// DbContexts
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))));
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))));

// Identity (Core) + SignInManager (necesario para inyectarlo en el AuthController)
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
    .AddSignInManager() // <- agrega SignInManager para resolver la inyección
    .AddDefaultTokenProviders();

// JWT (forzar como esquema por defecto para evitar challenge por cookies)
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

// Autorization: quitar FallbackPolicy para no proteger /swagger ni páginas inexistentes
builder.Services.AddAuthorization();

// App services
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

// Swagger + JWT
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

var app = builder.Build();

// Migraciones y seeding
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var r in new[] { "Admin", "Oferente" })
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole(r));

    var cfg = app.Configuration;
    var adminEmail = cfg["SeedAdmin:Email"];
    var adminPass = cfg["SeedAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPass))
    {
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var u = await userMgr.FindByEmailAsync(adminEmail);
        if (u is null)
        {
            u = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var res = await userMgr.CreateAsync(u, adminPass);
            if (res.Succeeded)
                await userMgr.AddToRoleAsync(u, "Admin");
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();