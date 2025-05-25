
var builder = DistributedApplication.CreateBuilder(args);

var frontoffice = builder.AddProject<Projects.MorWalPizVideo_Server>("app", "aspire");

builder.AddNpmApp("morwalpizvideo", "../morwalpizvideo.client", "dev")
    .WithReference(frontoffice)
    .WaitFor(frontoffice)
    .WithEnvironment("VITE_API_BASE_URL", frontoffice.GetEndpoint("https"))
    .WithEnvironment("BROWSER", "none")
    .WithHttpsEndpoint(port: 5174, env: "PORT",name: "https");

var backoffice = builder.AddProject<Projects.MorWalPizVideo_BackOffice>("backoffice");

builder.AddNpmApp("back-office-spa", "../BackOfficeSPA/back-office-spa", "dev")
    .WithReference(backoffice)
    .WaitFor(backoffice)
    .WithEnvironment("VITE_API_BASE_URL", backoffice.GetEndpoint("https"))
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(port: 5173,env: "PORT");

builder.Build().Run();
