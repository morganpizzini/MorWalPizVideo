
var builder = DistributedApplication.CreateBuilder(args);

var frontendGroup = builder.AddGroup("Frontend");
var backendGroup = builder.AddGroup("Backend");
var serviceGroup = builder.AddGroup("Services");

var frontoffice = builder.AddProject<Projects.MorWalPizVideo_ServerAPI>("server")
    .InGroup(frontendGroup);

builder.AddNpmApp("morwalpizvideo", "../morwalpizvideo.client", "dev")
    .WithReference(frontoffice)
    .WaitFor(frontoffice)
    .WithEnvironment("ASPNETCORE_URLS", frontoffice.GetEndpoint("https"))
    .WithEnvironment("BROWSER", "none")
    .WithHttpsEndpoint(port: 5174, env: "PORT",name: "https")
    .InGroup(frontendGroup);

var backoffice = builder.AddProject<Projects.MorWalPizVideo_BackOffice>("backoffice")
                    .InGroup(backendGroup);

builder.AddNpmApp("back-office-spa", "../BackOfficeSPA/back-office-spa", "dev")
    .WithReference(backoffice)
    .WaitFor(backoffice)
    .WithEnvironment("ASPNETCORE_URLS", backoffice.GetEndpoint("https"))
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(port: 5173,env: "PORT")
    .InGroup(backendGroup);

builder.AddProject<Projects.MorWalPizVideo_ShortLinks>("morwalpizvideo-shortlinks")
    .InGroup(serviceGroup);

builder.Build().Run();

static class AspireHostingExtensions
{
    public static IResourceBuilder<Resource> AddGroup(this IDistributedApplicationBuilder builder, string name) =>
        builder.AddResource(new GroupResource(name))
            .WithInitialState(new()
            {
                State = new(KnownResourceStates.Running, KnownResourceStateStyles.Success),
                ResourceType = "Group",
                Properties = []
            });

    public static IResourceBuilder<T> InGroup<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> group)
        where T : IResource
    {
        if (builder.Resource.TryGetAnnotationsOfType<ResourceSnapshotAnnotation>(out var annot))
        {
            foreach (var snapshot in annot)
            {
                snapshot.InitialSnapshot.GetType().GetProperty("Properties")?.SetValue(snapshot.InitialSnapshot,
                    snapshot.InitialSnapshot.Properties.Add(new("resource.parentName", "data")));
            }
        }
        else
        {
            builder.WithInitialState(new()
            {
                ResourceType = builder.Resource.GetType().Name ?? "Unknown",
                Properties =
                [
                    new("resource.parentName", group.Resource.Name),
                ]
            });
        }

        return builder;
    }


    class GroupResource(string name) : Resource(name)
    {
    }
}