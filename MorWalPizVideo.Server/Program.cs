var builder = WebApplication.CreateBuilder(args);

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapDefaultEndpoints();

// Map health check endpoint
app.MapHealthChecks("/health");

app.UseDefaultFiles();
app.MapStaticAssets();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapFallbackToFile("/index.html");

app.Run();
