var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MorWalPizVideo_Server>("morwalpizvideo-server");

builder.AddProject<Projects.MorWalPizVideo_BackOffice>("morwalpizvideo-backoffice");

builder.Build().Run();
