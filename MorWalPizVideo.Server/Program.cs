using Microsoft.AspNetCore.Http.Json;
using MorWalPizVideo.Server.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<DataService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var apiGroup = app.MapGroup("/api");

apiGroup.MapGet("/reset", (DataService dataService) =>
{
    dataService.ResetItems();
    return Results.Ok();
})
.WithName("Reset")
.WithOpenApi();

apiGroup.MapGet("/matches", (DataService dataService) =>
{
    return dataService.GetItems();
})
.WithName("Matches")
.WithOpenApi();

apiGroup.MapGet("/matches/{url}", (DataService dataService, string url) =>
{
    return dataService.GetItems().FirstOrDefault(x => x.Url == url);
})
.WithName("Match Detail")
.WithOpenApi();

apiGroup.MapGet("/products", (DataService dataService) =>
{
    return dataService.GetProducts();
})
.WithName("Products")
.WithOpenApi();

app.MapFallbackToFile("/index.html");

app.Run();

