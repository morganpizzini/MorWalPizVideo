var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MorWalPizVideo_Server>("morwalpizvideo-server");

builder.AddProject<Projects.MorWalPizVideo_Operations>("morwalpizvideo-operations");

builder.Build().Run();
