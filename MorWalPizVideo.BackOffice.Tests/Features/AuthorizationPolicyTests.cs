using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using MorWalPizVideo.MvcHelpers.Authentication;
using AuthController = MorWalPizVideo.BackOffice.Controllers.AuthController;
using ApplicationControllerBase = MorWalPizVideo.BackOffice.Controllers.ApplicationControllerBase;
using AdminDigitalProductsController = MorWalPizVideo.BackOffice.Controllers.AdminDigitalProductsController;
using AdminDigitalProductCategoriesController = MorWalPizVideo.BackOffice.Controllers.AdminDigitalProductCategoriesController;
using DigitalProductsController = MorWalPizVideo.BackOffice.Controllers.DigitalProductsController;
using DigitalProductCategoriesController = MorWalPizVideo.BackOffice.Controllers.DigitalProductCategoriesController;
using BioLinksController = MorWalPizVideo.ServerAPI.Controllers.BioLinksController;
using CalendarEventsController = MorWalPizVideo.ServerAPI.Controllers.CalendarEventsController;
using CacheController = MorWalPizVideo.ServerAPI.Controllers.CacheController;
using CompetitionsController = MorWalPizVideo.ServerAPI.Controllers.CompetitionsController;
using CompilationsController = MorWalPizVideo.ServerAPI.Controllers.CompilationsController;
using CustomFormsController = MorWalPizVideo.ServerAPI.Controllers.CustomFormsController;
using MatchesController = MorWalPizVideo.ServerAPI.Controllers.MatchesController;
using PagesController = MorWalPizVideo.ServerAPI.Controllers.PagesController;
using ProductsController = MorWalPizVideo.ServerAPI.Controllers.ProductsController;
using ShopCatalogController = MorWalPizVideo.ServerAPI.Controllers.ShopCatalogController;
using SponsorsController = MorWalPizVideo.ServerAPI.Controllers.SponsorsController;

namespace MorWalPizVideo.BackOffice.Tests.Features;

// ADR-002 (Explicit Host Authentication): regression guards for the shared base and the
// most security-sensitive endpoints across BackOffice/ServerAPI hosts.
public class AuthorizationPolicyTests
{
    [Fact]
    public void ApplicationControllerBase_is_host_neutral()
    {
        var attributes = typeof(ApplicationControllerBase).GetCustomAttributes<AuthorizeAttribute>(inherit: false);
        Assert.Empty(attributes);
    }

    [Fact]
    public void AuthController_allows_anonymous_login()
    {
        Assert.NotEmpty(typeof(AuthController).GetCustomAttributes<AllowAnonymousAttribute>(inherit: false));
    }

    [Fact]
    public void CacheController_requires_internal_service_identity()
    {
        var authorize = typeof(CacheController).GetCustomAttributes<AuthorizeAttribute>(inherit: false).Single();
        Assert.Equal(InternalServiceAuthenticationHandler.SchemeName, authorize.AuthenticationSchemes);
    }

    [Theory]
    [InlineData(typeof(BioLinksController))]
    [InlineData(typeof(CalendarEventsController))]
    [InlineData(typeof(CompetitionsController))]
    [InlineData(typeof(CompilationsController))]
    [InlineData(typeof(CustomFormsController))]
    [InlineData(typeof(MatchesController))]
    [InlineData(typeof(PagesController))]
    [InlineData(typeof(ProductsController))]
    [InlineData(typeof(SponsorsController))]
    public void ServerApi_public_content_controllers_allow_anonymous(Type controllerType)
    {
        Assert.NotEmpty(controllerType.GetCustomAttributes<AllowAnonymousAttribute>(inherit: false));
    }

    [Fact]
    public void ServerApi_ConfigurationController_allows_anonymous()
    {
        Assert.NotEmpty(typeof(MorWalPizVideo.Server.Controllers.ConfigurationController).GetCustomAttributes<AllowAnonymousAttribute>(inherit: false));
    }

    [Fact]
    public void Legacy_shop_catalog_routes_remain_public_and_anonymous()
    {
        var controllerType = typeof(ShopCatalogController);
        Assert.NotEmpty(controllerType.GetCustomAttributes<AllowAnonymousAttribute>(inherit: false));
        Assert.Empty(controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: false));
    }

    [Fact]
    public void BackOffice_digital_product_admin_routes_require_authorization()
    {
        var adminProductsController = typeof(AdminDigitalProductsController);
        var legacyProductsController = typeof(DigitalProductsController);

        Assert.NotEmpty(adminProductsController.GetCustomAttributes<AuthorizeAttribute>(inherit: false));
        Assert.NotEmpty(legacyProductsController.GetCustomAttributes<AuthorizeAttribute>(inherit: false));
        Assert.Empty(adminProductsController.GetCustomAttributes<AllowAnonymousAttribute>(inherit: false));
        Assert.Empty(legacyProductsController.GetCustomAttributes<AllowAnonymousAttribute>(inherit: false));
    }

    [Fact]
    public void BackOffice_digital_product_category_admin_routes_require_authorization()
    {
        var adminCategoriesController = typeof(AdminDigitalProductCategoriesController);
        var legacyCategoriesController = typeof(DigitalProductCategoriesController);

        Assert.NotEmpty(adminCategoriesController.GetCustomAttributes<AuthorizeAttribute>(inherit: false));
        Assert.NotEmpty(legacyCategoriesController.GetCustomAttributes<AuthorizeAttribute>(inherit: false));
        Assert.Empty(adminCategoriesController.GetCustomAttributes<AllowAnonymousAttribute>(inherit: false));
        Assert.Empty(legacyCategoriesController.GetCustomAttributes<AllowAnonymousAttribute>(inherit: false));
    }
}
