var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var matches = new[]
{
    new Match("VqOSvayqUMM", "Campionato Italiano 2024", "Gara IPSC in cui ho spaccato tutto!","italiano-2024",
    [
        new Video("ZFpHU2cFXn0?si=_bIDLsQbfr2PZFu5", "Da DUE, ne uscirá UNA giusta? - VLOG IPSC FINALE ITALIANO", "Video blog per la FINALE ITALIANA IPSC, allenamento, produzione dei colpi [..]" ),
        new Video("VqOSvayqUMM?si=xJhZxB4szb-ux9wX", "HO spaccato TUTTO!! PROPRIO in FINALE", "Se vi state chiedendo cosa ho rotto questa volta... la risposta è [..]" ),
        new Video("XqUGRMjTrpM?si=MJp8W2S4YfrKVtrQ", "Una gara TRISTE, sia per il TEMPO, che per le PRESTAZIONI", "Mi sono ritagliato qualche minuto per fare un`analisi della mia gara dallo stage [..]" ),
        new Video("ZLM_F0hsqbo?si=Rvd4zu7H0OT2ALGm", "Sparare con RISO ed OKI, perchè il PODIO non aspetta nessuno", "Seconda sezione della review degli stage" )
    ]),
};

var apiGroup = app.MapGroup("/api");

apiGroup.MapGet("/matches", () =>
{
    return matches;
})
.WithName("Matches")
.WithOpenApi();

apiGroup.MapGet("/matches/{url}", (string url) =>
{
    return matches.FirstOrDefault(x => x.Url == url);
})
.WithName("Match")
.WithOpenApi();

app.MapFallbackToFile("/index.html");

app.Run();

internal record Match(string ThumbnailUrl, string Title, string Description, string Url, Video[] Videos)
{
}
internal record Video(string Id, string Title, string Description)
{
}
