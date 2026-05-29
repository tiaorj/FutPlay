using FutPlay.Data;
using FutPlay.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using FutPlay.Models.Api;
using FutPlay.Settings;
using Serilog;
using System.Threading.RateLimiting;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Logging.json", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();
});

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Muitas requisições. Tente novamente mais tarde.",
            cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/Identity") ||
            httpContext.Request.Path.StartsWithSegments("/health"))
        {
            return RateLimitPartition.GetNoLimiter("unlimited");
        }

        return CriarParticaoRateLimit(httpContext, permitLimit: 120, window: TimeSpan.FromMinutes(1));
    });

    options.AddPolicy("General", httpContext =>
        CriarParticaoRateLimit(httpContext, permitLimit: 120, window: TimeSpan.FromMinutes(1)));

    options.AddPolicy("CriticalActions", httpContext =>
        CriarParticaoRateLimit(httpContext, permitLimit: 5, window: TimeSpan.FromMinutes(1)));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

builder.Services.Configure<ApiFootballOptions>(
    builder.Configuration.GetSection("ApiFootball"));

builder.Services.Configure<FootballDataOrgOptions>(
    builder.Configuration.GetSection("FootballDataOrg"));

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection("Email"));

builder.Services.Configure<AppTimeOptions>(
    builder.Configuration.GetSection("AppTimeZone"));

builder.Services.AddHttpClient<FootballApiService>();
builder.Services.AddHttpClient<FootballDataOrgService>();
builder.Services.AddScoped<ImportacaoCampeonatoService>();
builder.Services.AddScoped<ImportacaoJogosService>();
builder.Services.AddScoped<ImportacaoResultadosService>();
builder.Services.AddScoped<CampeonatoSincronizacaoService>();
builder.Services.AddScoped<ClassificacaoService>();
builder.Services.AddScoped<PontuacaoService>();
builder.Services.AddScoped<MockDataService>();
builder.Services.AddScoped<ImportacaoTimesService>();
builder.Services.AddScoped<ApiSyncLogService>();
builder.Services.AddScoped<ConviteEmailService>();
builder.Services.AddScoped<AnalisePartidaService>();
builder.Services.AddScoped<ComparadorTimesService>();
builder.Services.AddScoped<RadarRodadaService>();
builder.Services.AddScoped<PalpiteBloqueioService>();
builder.Services.AddScoped<PalpiteComunidadeService>();
builder.Services.AddSingleton<AppTimeService>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("sqlserver");

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    options.User.RequireUniqueEmail = true;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

await IdentitySeedService.SeedAsync(app.Services, app.Configuration);

// Configure Request Localization (pt-BR) - aplica cultura globalmente
var supportedCultures = new[] { new CultureInfo("pt-BR") };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("pt-BR"),
    SupportedCultures = supportedCultures.ToList(),
    SupportedUICultures = supportedCultures.ToList()
};
app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<EnsureParticipantRoleMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString()
        });
    }
});

app.Run();

static RateLimitPartition<string> CriarParticaoRateLimit(
    HttpContext httpContext,
    int permitLimit,
    TimeSpan window)
{
    var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString();

    if (string.IsNullOrWhiteSpace(partitionKey))
    {
        partitionKey = "anonymous";
    }

    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey,
        _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = permitLimit,
            QueueLimit = 0,
            Window = window
        });
}
