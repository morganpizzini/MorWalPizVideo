using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MorWalPizVideo.Domain.ShootingRange;
using MorWalPizVideo.Models.ShootingRange;
using MorWalPizVideo.ShootingRange.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; options.Cookie.Name = "__Host-shooting-range-csrf"; options.Cookie.HttpOnly = false; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.None; });
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => { options.Cookie.Name = "shooting_range_auth"; options.Cookie.HttpOnly = true; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.None; options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }; });
builder.Services.AddAuthorization();
var useMock = builder.Configuration.GetValue("FeatureManagement:EnableMock", builder.Environment.IsDevelopment());
if (useMock) builder.Services.AddSingleton<IShootingRangeRepositorySet, MockShootingRangeRepositorySet>();
else
{
    builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(builder.Configuration["MorWalPizDatabase:ConnectionString"] ?? throw new InvalidOperationException("MorWalPizDatabase:ConnectionString is required.")));
    builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration["MorWalPizDatabase:DatabaseName"] ?? "morwalpizvideo"));
    builder.Services.AddSingleton<IShootingRangeRepositorySet, MongoShootingRangeRepositorySet>();
}
builder.Services.AddScoped<ShootingRangeService>();
var app = builder.Build();
app.MapDefaultEndpoints();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
