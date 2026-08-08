namespace MorWalPizVideo.Models.Constraints;

public static class AuthorizationPermissionKeys
{
  public const string BackofficeAccess = "backoffice.access";
  public const string BackofficeManageAll = "backoffice.manageall";
  public const string BackofficeImpersonate = "backoffice.impersonate";

  public const string UsersView = "users.view";
  public const string UsersManage = "users.manage";
  public const string UsersCreate = "users.create";
  public const string UsersUpdate = "users.update";
  public const string UsersDelete = "users.delete";
  public const string UsersPermissionsManage = "users.permissions.manage";

  public const string VideosView = "videos.view";
  public const string VideosManage = "videos.manage";
  public const string VideosCreate = "videos.create";
  public const string VideosUpdate = "videos.update";
  public const string VideosDelete = "videos.delete";
  public const string VideosImport = "videos.import";
  public const string VideosTranslate = "videos.translate";
  public const string VideosPublish = "videos.publish";

  public const string ChannelsView = "channels.view";
  public const string ChannelsManage = "channels.manage";
  public const string ChannelsCreate = "channels.create";
  public const string ChannelsUpdate = "channels.update";
  public const string ChannelsDelete = "channels.delete";

  public const string CategoriesView = "categories.view";
  public const string CategoriesManage = "categories.manage";
  public const string CategoriesCreate = "categories.create";
  public const string CategoriesUpdate = "categories.update";
  public const string CategoriesDelete = "categories.delete";

  public const string ImagesView = "images.view";
  public const string ImagesManage = "images.manage";
  public const string ImagesCreate = "images.create";
  public const string ImagesDelete = "images.delete";

  public const string CalendarView = "calendar.view";
  public const string CalendarManage = "calendar.manage";
  public const string CalendarCreate = "calendar.create";
  public const string CalendarUpdate = "calendar.update";
  public const string CalendarDelete = "calendar.delete";

  public const string ShortLinksView = "shortlinks.view";
  public const string ShortLinksManage = "shortlinks.manage";
  public const string ShortLinksCreate = "shortlinks.create";
  public const string ShortLinksUpdate = "shortlinks.update";
  public const string ShortLinksDelete = "shortlinks.delete";

  public const string QueryLinksView = "querylinks.view";
  public const string QueryLinksManage = "querylinks.manage";
  public const string QueryLinksCreate = "querylinks.create";
  public const string QueryLinksUpdate = "querylinks.update";
  public const string QueryLinksDelete = "querylinks.delete";

  public const string FormsView = "forms.view";
  public const string FormsManage = "forms.manage";
  public const string FormsCreate = "forms.create";
  public const string FormsUpdate = "forms.update";
  public const string FormsDelete = "forms.delete";
  public const string FormsResponsesView = "forms.responses.view";
  public const string InsightsView = "insights.view";
  public const string InsightsManage = "insights.manage";
  public const string InsightsCreate = "insights.create";
  public const string InsightsUpdate = "insights.update";
  public const string InsightsDelete = "insights.delete";
  public const string InsightsScan = "insights.scan";

  public const string ApiKeysView = "apikeys.view";
  public const string ApiKeysManage = "apikeys.manage";
  public const string ApiKeysCreate = "apikeys.create";
  public const string ApiKeysUpdate = "apikeys.update";
  public const string ApiKeysDelete = "apikeys.delete";

  public const string ConfigurationsView = "configurations.view";
  public const string ConfigurationsManage = "configurations.manage";
  public const string ConfigurationsCreate = "configurations.create";
  public const string ConfigurationsUpdate = "configurations.update";
  public const string ConfigurationsDelete = "configurations.delete";

  public const string ProductCategoriesView = "productcategories.view";
  public const string ProductCategoriesManage = "productcategories.manage";
  public const string ProductCategoriesCreate = "productcategories.create";
  public const string ProductCategoriesUpdate = "productcategories.update";
  public const string ProductCategoriesDelete = "productcategories.delete";

  public const string SponsorsView = "sponsors.view";
  public const string SponsorsManage = "sponsors.manage";
  public const string SponsorsCreate = "sponsors.create";
  public const string SponsorsUpdate = "sponsors.update";
  public const string SponsorsDelete = "sponsors.delete";

  public const string ProductsView = "products.view";
  public const string ProductsManage = "products.manage";
  public const string ProductsCreate = "products.create";
  public const string ProductsUpdate = "products.update";
  public const string ProductsDelete = "products.delete";

  public const string CompilationsView = "compilations.view";
  public const string CompilationsManage = "compilations.manage";
  public const string CompilationsCreate = "compilations.create";
  public const string CompilationsUpdate = "compilations.update";
  public const string CompilationsDelete = "compilations.delete";

  public const string DiagnosticsView = "diagnostics.view";
}

public static class AuthorizationGroupCodes
{
  // Canonical lowercase group codes.
  public const string Admin = "admin";
  public const string Contributor = "contributor";
}
