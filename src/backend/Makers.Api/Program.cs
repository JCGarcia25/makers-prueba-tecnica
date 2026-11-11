using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Makers.Api;
using Makers.Application;
using Makers.Domain;
using Makers.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddMemoryCache(options => options.SizeLimit = 1_000);
builder.Services.AddSingleton<LoanCache>();
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddDbContext<LoanDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddScoped<ILoanStore, LoanStore>();
builder.Services.AddScoped<IUserStore, UserStore>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no está configurado.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var store = context.HttpContext.RequestServices.GetRequiredService<IUserStore>();
            var idValue = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var versionValue = context.Principal?.FindFirst("security_version")?.Value;
            if (!Guid.TryParse(idValue, out var id) || !Guid.TryParse(versionValue, out var version))
            {
                context.Fail("Token inválido.");
                return;
            }
            var user = await store.FindAsync(id, context.HttpContext.RequestAborted);
            if (user is null || !user.IsActive || user.SecurityVersion != version) context.Fail("La sesión ya no es válida.");
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

var auth = app.MapGroup("/api/auth");
auth.MapPost("/login", async (LoginRequest request, IUserStore users, IPasswordService passwords,
    JwtTokenService tokens, CancellationToken ct) =>
{
    var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
    if (user is null || !user.IsActive || !passwords.Verify(user, request.Password))
        throw new BusinessException("Credenciales inválidas.", 401);
    return Results.Ok(tokens.Create(user));
});
auth.MapPost("/register", async (UserInput input, UserService users, JwtTokenService tokens, CancellationToken ct) =>
    Results.Created("/api/users/me", tokens.Create(await users.CreateAsync(input, ct))));

var loans = app.MapGroup("/api/loans").RequireAuthorization();
loans.MapGet("/", async (HttpContext context, ILoanStore store, int page = 1, int size = 20,
    string? status = null, CancellationToken ct = default) =>
{
    ValidatePage(page, size);
    var parsed = ParseStatus(status);
    Guid? ownerId = context.User.IsAdmin() ? null : context.User.UserId();
    return Results.Ok(await store.ListAsync(ownerId, page, size, parsed, ct));
});
loans.MapGet("/{id:guid}", async (Guid id, HttpContext context, LoanService service, CancellationToken ct) =>
    Results.Ok(await service.ReadAsync(id, context.User.UserId(), context.User.IsAdmin(), ct)));
loans.MapPost("/", async (LoanInput input, HttpContext context, LoanService service, CancellationToken ct) =>
{
    var result = await service.CreateAsync(context.User.UserId(), input, ct);
    return Results.Created($"/api/loans/{result.Id}", result);
});
loans.MapPut("/{id:guid}", async (Guid id, LoanInput input, HttpContext context, LoanService service, CancellationToken ct) =>
    Results.Ok(await service.UpdateAsync(id, context.User.UserId(), context.User.IsAdmin(), input, ct)));
loans.MapDelete("/{id:guid}", async (Guid id, HttpContext context, LoanService service, CancellationToken ct) =>
{
    await service.DeleteAsync(id, context.User.UserId(), context.User.IsAdmin(), ct);
    return Results.NoContent();
});
loans.MapPatch("/{id:guid}/decision", async (Guid id, DecisionInput input, HttpContext context,
    LoanService service, CancellationToken ct) => Results.Ok(await service.DecideAsync(id, context.User.UserId(), input, ct)))
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

var usersGroup = app.MapGroup("/api/users").RequireAuthorization(policy => policy.RequireRole("Admin"));
usersGroup.MapGet("/", async (IUserStore users, int page = 1, int size = 20, CancellationToken ct = default) =>
{
    ValidatePage(page, size);
    return Results.Ok(await users.ListAsync(page, size, ct));
});
usersGroup.MapPost("/", async (UserInput input, UserService users, CancellationToken ct) =>
{
    var user = await users.CreateAsync(input, ct);
    return Results.Created($"/api/users/{user.Id}", UserService.View(user));
});
usersGroup.MapPut("/{id:guid}", async (Guid id, UserUpdate input, UserService users, CancellationToken ct) =>
    Results.Ok(await users.UpdateAsync(id, input, ct)));
usersGroup.MapDelete("/{id:guid}", async (Guid id, HttpContext context, UserService users, CancellationToken ct) =>
{
    await users.DeactivateAsync(id, context.User.UserId(), ct);
    return Results.NoContent();
});

app.MapGet("/api/analytics/loans", async (ILoanStore loans, CancellationToken ct) => Results.Ok(await loans.ExportAsync(ct)))
    .RequireAuthorization(policy => policy.RequireRole("Admin"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
    if (db.Database.IsRelational()) await db.Database.MigrateAsync();
    else await db.Database.EnsureCreatedAsync();
    if (app.Configuration.GetValue<bool>("Seed:Enabled"))
        await DemoSeed.RunAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordService>(),
            app.Configuration["Seed:Password"] ?? "", app.Lifetime.ApplicationStopping);
}

app.Run();

static void ValidatePage(int page, int size)
{
    if (page < 1 || size is < 1 or > 100) throw new BusinessException("La página debe ser positiva y el tamaño estar entre 1 y 100.");
}

static LoanStatus? ParseStatus(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return null;
    return Enum.TryParse<LoanStatus>(value, true, out var status)
        ? status
        : throw new BusinessException("Estado inválido.");
}

public partial class Program;
