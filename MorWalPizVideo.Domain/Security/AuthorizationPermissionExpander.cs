using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.Domain.Security;

public static class AuthorizationPermissionExpander
{
  private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Implications =
      new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
      {
        [AuthorizationPermissionKeys.BackofficeManageAll] =
        [
          AuthorizationPermissionKeys.BackofficeAccess,
          AuthorizationPermissionKeys.BackofficeImpersonate
        ],
        [AuthorizationPermissionKeys.UsersManage] =
        [
          AuthorizationPermissionKeys.UsersView,
          AuthorizationPermissionKeys.UsersCreate,
          AuthorizationPermissionKeys.UsersUpdate,
          AuthorizationPermissionKeys.UsersDelete,
          AuthorizationPermissionKeys.UsersPermissionsManage
        ],
        [AuthorizationPermissionKeys.VideosManage] =
        [
          AuthorizationPermissionKeys.VideosView,
          AuthorizationPermissionKeys.VideosCreate,
          AuthorizationPermissionKeys.VideosUpdate,
          AuthorizationPermissionKeys.VideosDelete,
          AuthorizationPermissionKeys.VideosImport,
          AuthorizationPermissionKeys.VideosTranslate,
          AuthorizationPermissionKeys.VideosPublish
        ],
        [AuthorizationPermissionKeys.ChannelsManage] =
        [
          AuthorizationPermissionKeys.ChannelsView,
          AuthorizationPermissionKeys.ChannelsCreate,
          AuthorizationPermissionKeys.ChannelsUpdate,
          AuthorizationPermissionKeys.ChannelsDelete
        ],
        [AuthorizationPermissionKeys.CategoriesManage] =
        [
          AuthorizationPermissionKeys.CategoriesView,
          AuthorizationPermissionKeys.CategoriesCreate,
          AuthorizationPermissionKeys.CategoriesUpdate,
          AuthorizationPermissionKeys.CategoriesDelete
        ],
        [AuthorizationPermissionKeys.ImagesManage] =
        [
          AuthorizationPermissionKeys.ImagesView,
          AuthorizationPermissionKeys.ImagesCreate,
          AuthorizationPermissionKeys.ImagesDelete
        ],
        [AuthorizationPermissionKeys.CalendarManage] =
        [
          AuthorizationPermissionKeys.CalendarView,
          AuthorizationPermissionKeys.CalendarCreate,
          AuthorizationPermissionKeys.CalendarUpdate,
          AuthorizationPermissionKeys.CalendarDelete
        ],
        [AuthorizationPermissionKeys.ShortLinksManage] =
        [
          AuthorizationPermissionKeys.ShortLinksView,
          AuthorizationPermissionKeys.ShortLinksCreate,
          AuthorizationPermissionKeys.ShortLinksUpdate,
          AuthorizationPermissionKeys.ShortLinksDelete
        ],
        [AuthorizationPermissionKeys.QueryLinksManage] =
        [
          AuthorizationPermissionKeys.QueryLinksView,
          AuthorizationPermissionKeys.QueryLinksCreate,
          AuthorizationPermissionKeys.QueryLinksUpdate,
          AuthorizationPermissionKeys.QueryLinksDelete
        ],
        [AuthorizationPermissionKeys.FormsManage] =
        [
          AuthorizationPermissionKeys.FormsView,
          AuthorizationPermissionKeys.FormsCreate,
          AuthorizationPermissionKeys.FormsUpdate,
          AuthorizationPermissionKeys.FormsDelete,
          AuthorizationPermissionKeys.FormsResponsesView
        ],
        [AuthorizationPermissionKeys.InsightsManage] =
        [
          AuthorizationPermissionKeys.InsightsView,
          AuthorizationPermissionKeys.InsightsCreate,
          AuthorizationPermissionKeys.InsightsUpdate,
          AuthorizationPermissionKeys.InsightsDelete,
          AuthorizationPermissionKeys.InsightsScan
        ],
        [AuthorizationPermissionKeys.ApiKeysManage] =
        [
          AuthorizationPermissionKeys.ApiKeysView,
          AuthorizationPermissionKeys.ApiKeysCreate,
          AuthorizationPermissionKeys.ApiKeysUpdate,
          AuthorizationPermissionKeys.ApiKeysDelete
        ],
        [AuthorizationPermissionKeys.ConfigurationsManage] =
        [
          AuthorizationPermissionKeys.ConfigurationsView,
          AuthorizationPermissionKeys.ConfigurationsCreate,
          AuthorizationPermissionKeys.ConfigurationsUpdate,
          AuthorizationPermissionKeys.ConfigurationsDelete
        ],
        [AuthorizationPermissionKeys.ProductCategoriesManage] =
        [
          AuthorizationPermissionKeys.ProductCategoriesView,
          AuthorizationPermissionKeys.ProductCategoriesCreate,
          AuthorizationPermissionKeys.ProductCategoriesUpdate,
          AuthorizationPermissionKeys.ProductCategoriesDelete
        ],
        [AuthorizationPermissionKeys.SponsorsManage] =
        [
          AuthorizationPermissionKeys.SponsorsView,
          AuthorizationPermissionKeys.SponsorsCreate,
          AuthorizationPermissionKeys.SponsorsUpdate,
          AuthorizationPermissionKeys.SponsorsDelete
        ],
        [AuthorizationPermissionKeys.ProductsManage] =
        [
          AuthorizationPermissionKeys.ProductsView,
          AuthorizationPermissionKeys.ProductsCreate,
          AuthorizationPermissionKeys.ProductsUpdate,
          AuthorizationPermissionKeys.ProductsDelete
        ],
        [AuthorizationPermissionKeys.CompilationsManage] =
        [
          AuthorizationPermissionKeys.CompilationsView,
          AuthorizationPermissionKeys.CompilationsCreate,
          AuthorizationPermissionKeys.CompilationsUpdate,
          AuthorizationPermissionKeys.CompilationsDelete
        ]
      };

  public static IReadOnlySet<string> Expand(IEnumerable<string>? permissions)
  {
    var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var pending = new Queue<string>();

    foreach (var permission in permissions ?? [])
    {
      var normalized = Normalize(permission);
      if (!string.IsNullOrWhiteSpace(normalized) && expanded.Add(normalized))
      {
        pending.Enqueue(normalized);
      }
    }

    while (pending.TryDequeue(out var permission))
    {
      if (!Implications.TryGetValue(permission, out var impliedPermissions))
      {
        continue;
      }

      foreach (var impliedPermission in impliedPermissions)
      {
        var normalized = Normalize(impliedPermission);
        if (expanded.Add(normalized))
        {
          pending.Enqueue(normalized);
        }
      }
    }

    return expanded;
  }

  public static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
}