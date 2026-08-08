using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Controllers;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class AdminRouteAuthorizationMetadataTests
{
    [Fact]
    public void Spa_routed_actions_have_explicit_authorization_metadata()
    {
        Type[] controllerTypes =
        [
            typeof(ApiKeysController), typeof(CalendarEventsController), typeof(CategoriesController),
            typeof(ChannelsController), typeof(CompilationsController), typeof(ConfigurationController),
            typeof(CustomFormsController), typeof(DiagnosticsController), typeof(ImageUploadController),
            typeof(InsightsController), typeof(ProductCategoriesController), typeof(ProductsController),
            typeof(QueryLinksController), typeof(RbacController), typeof(ShortLinksController),
            typeof(SponsorsController), typeof(UserController), typeof(VideosController)
        ];

        var unsecuredActions = controllerTypes
            .SelectMany(controllerType => controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Where(method => !HasExplicitAuthorization(controllerType, method))
                .Select(method => $"{controllerType.Name}.{method.Name}"))
            .OrderBy(name => name)
            .ToArray();

        Assert.Empty(unsecuredActions);
    }

    private static bool HasExplicitAuthorization(Type controllerType, MethodInfo method) =>
        method.GetCustomAttribute<AllowAnonymousAttribute>() is not null ||
        method.GetCustomAttribute<AllowUserAttribute>() is not null ||
        method.GetCustomAttribute<ApiKeyAuthAttribute>() is not null ||
        controllerType.GetCustomAttribute<AllowUserAttribute>() is not null ||
        controllerType.GetCustomAttribute<ApiKeyAuthAttribute>() is not null;
}