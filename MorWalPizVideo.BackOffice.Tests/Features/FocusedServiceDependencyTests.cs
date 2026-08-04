using System.Reflection;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class FocusedServiceDependencyTests
{
    [Fact]
    public void HighImpactControllers_DoNotDependOnDataService()
    {
        Assert.DoesNotContain(GetConstructorTypes(typeof(ShortLinksController)), t => t == typeof(DataService));
        Assert.DoesNotContain(GetConstructorTypes(typeof(VideosController)), t => t == typeof(DataService));
        Assert.DoesNotContain(GetConstructorTypes(typeof(CompilationsController)), t => t == typeof(DataService));
    }

    [Fact]
    public void HighImpactControllers_UseFocusedServices()
    {
        Assert.Contains(typeof(ILinksService), GetConstructorTypes(typeof(ShortLinksController)));
        Assert.Contains(typeof(IContentService), GetConstructorTypes(typeof(ShortLinksController)));

        Assert.Contains(typeof(IContentService), GetConstructorTypes(typeof(VideosController)));
        Assert.Contains(typeof(ILinksService), GetConstructorTypes(typeof(VideosController)));

        Assert.Contains(typeof(ICatalogService), GetConstructorTypes(typeof(CompilationsController)));
        Assert.Contains(typeof(IContentService), GetConstructorTypes(typeof(CompilationsController)));
    }

    private static Type[] GetConstructorTypes(Type controllerType)
    {
        var constructor = controllerType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .Single();

        return constructor
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToArray();
    }
}
