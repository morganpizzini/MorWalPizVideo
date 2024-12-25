var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MorWalPizVideo_Server>("app");

builder.AddProject<Projects.MorWalPizVideo_BackOffice>("backoffice");

builder.Build().Run();
