using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class AuthorizationPermissionExpanderTests
{
  public static TheoryData<string, string[]> ManagePermissionImplications => new()
  {
    { AuthorizationPermissionKeys.UsersManage, [AuthorizationPermissionKeys.UsersView, AuthorizationPermissionKeys.UsersCreate, AuthorizationPermissionKeys.UsersUpdate, AuthorizationPermissionKeys.UsersDelete, AuthorizationPermissionKeys.UsersPermissionsManage] },
    { AuthorizationPermissionKeys.VideosManage, [AuthorizationPermissionKeys.VideosView, AuthorizationPermissionKeys.VideosCreate, AuthorizationPermissionKeys.VideosUpdate, AuthorizationPermissionKeys.VideosDelete, AuthorizationPermissionKeys.VideosImport, AuthorizationPermissionKeys.VideosTranslate, AuthorizationPermissionKeys.VideosPublish] },
    { AuthorizationPermissionKeys.ChannelsManage, [AuthorizationPermissionKeys.ChannelsView, AuthorizationPermissionKeys.ChannelsCreate, AuthorizationPermissionKeys.ChannelsUpdate, AuthorizationPermissionKeys.ChannelsDelete] },
    { AuthorizationPermissionKeys.CategoriesManage, [AuthorizationPermissionKeys.CategoriesView, AuthorizationPermissionKeys.CategoriesCreate, AuthorizationPermissionKeys.CategoriesUpdate, AuthorizationPermissionKeys.CategoriesDelete] },
    { AuthorizationPermissionKeys.ImagesManage, [AuthorizationPermissionKeys.ImagesView, AuthorizationPermissionKeys.ImagesCreate, AuthorizationPermissionKeys.ImagesDelete] },
    { AuthorizationPermissionKeys.CalendarManage, [AuthorizationPermissionKeys.CalendarView, AuthorizationPermissionKeys.CalendarCreate, AuthorizationPermissionKeys.CalendarUpdate, AuthorizationPermissionKeys.CalendarDelete] },
    { AuthorizationPermissionKeys.ShortLinksManage, [AuthorizationPermissionKeys.ShortLinksView, AuthorizationPermissionKeys.ShortLinksCreate, AuthorizationPermissionKeys.ShortLinksUpdate, AuthorizationPermissionKeys.ShortLinksDelete] },
    { AuthorizationPermissionKeys.QueryLinksManage, [AuthorizationPermissionKeys.QueryLinksView, AuthorizationPermissionKeys.QueryLinksCreate, AuthorizationPermissionKeys.QueryLinksUpdate, AuthorizationPermissionKeys.QueryLinksDelete] },
    { AuthorizationPermissionKeys.FormsManage, [AuthorizationPermissionKeys.FormsView, AuthorizationPermissionKeys.FormsCreate, AuthorizationPermissionKeys.FormsUpdate, AuthorizationPermissionKeys.FormsDelete, AuthorizationPermissionKeys.FormsResponsesView] },
    { AuthorizationPermissionKeys.InsightsManage, [AuthorizationPermissionKeys.InsightsView, AuthorizationPermissionKeys.InsightsCreate, AuthorizationPermissionKeys.InsightsUpdate, AuthorizationPermissionKeys.InsightsDelete, AuthorizationPermissionKeys.InsightsScan] },
    { AuthorizationPermissionKeys.ApiKeysManage, [AuthorizationPermissionKeys.ApiKeysView, AuthorizationPermissionKeys.ApiKeysCreate, AuthorizationPermissionKeys.ApiKeysUpdate, AuthorizationPermissionKeys.ApiKeysDelete] },
    { AuthorizationPermissionKeys.ConfigurationsManage, [AuthorizationPermissionKeys.ConfigurationsView, AuthorizationPermissionKeys.ConfigurationsCreate, AuthorizationPermissionKeys.ConfigurationsUpdate, AuthorizationPermissionKeys.ConfigurationsDelete] },
    { AuthorizationPermissionKeys.ProductCategoriesManage, [AuthorizationPermissionKeys.ProductCategoriesView, AuthorizationPermissionKeys.ProductCategoriesCreate, AuthorizationPermissionKeys.ProductCategoriesUpdate, AuthorizationPermissionKeys.ProductCategoriesDelete] },
    { AuthorizationPermissionKeys.SponsorsManage, [AuthorizationPermissionKeys.SponsorsView, AuthorizationPermissionKeys.SponsorsCreate, AuthorizationPermissionKeys.SponsorsUpdate, AuthorizationPermissionKeys.SponsorsDelete] },
    { AuthorizationPermissionKeys.ProductsManage, [AuthorizationPermissionKeys.ProductsView, AuthorizationPermissionKeys.ProductsCreate, AuthorizationPermissionKeys.ProductsUpdate, AuthorizationPermissionKeys.ProductsDelete] },
    { AuthorizationPermissionKeys.CompilationsManage, [AuthorizationPermissionKeys.CompilationsView, AuthorizationPermissionKeys.CompilationsCreate, AuthorizationPermissionKeys.CompilationsUpdate, AuthorizationPermissionKeys.CompilationsDelete] }
  };

  [Theory]
  [MemberData(nameof(ManagePermissionImplications))]
  public void Manage_permission_implies_exact_declared_sibling_capabilities(
      string managePermission,
      string[] impliedPermissions)
  {
    var expanded = AuthorizationPermissionExpander.Expand([$" {managePermission.ToUpperInvariant()} "]);
    var expected = impliedPermissions.Append(managePermission).ToHashSet(StringComparer.OrdinalIgnoreCase);

    Assert.Equal(expected, expanded);
    Assert.All(expanded, permission => Assert.Equal(permission.ToLowerInvariant(), permission));
  }

  [Theory]
  [InlineData(AuthorizationPermissionKeys.VideosCreate)]
  [InlineData(AuthorizationPermissionKeys.FormsResponsesView)]
  [InlineData(AuthorizationPermissionKeys.InsightsScan)]
  [InlineData(AuthorizationPermissionKeys.UsersPermissionsManage)]
  [InlineData(AuthorizationPermissionKeys.DiagnosticsView)]
  public void Leaf_permission_does_not_imply_parent_siblings_or_other_resources(string permission)
  {
    var expanded = AuthorizationPermissionExpander.Expand([permission]);

    Assert.Single(expanded);
    Assert.Contains(permission, expanded);
  }

  [Fact]
  public void Backoffice_manageall_materializes_access_and_impersonate_and_preserves_global_permission()
  {
    var expanded = AuthorizationPermissionExpander.Expand(
        [AuthorizationPermissionKeys.BackofficeManageAll]);

    Assert.Equal(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
          AuthorizationPermissionKeys.BackofficeManageAll,
          AuthorizationPermissionKeys.BackofficeAccess,
          AuthorizationPermissionKeys.BackofficeImpersonate
        },
        expanded);
  }

  [Fact]
  public void Expansion_ignores_null_blank_and_duplicate_permissions()
  {
    var expanded = AuthorizationPermissionExpander.Expand(
        [null!, " ", " VIDEOS.MANAGE ", AuthorizationPermissionKeys.VideosManage]);

    Assert.Equal(8, expanded.Count);
  }
}