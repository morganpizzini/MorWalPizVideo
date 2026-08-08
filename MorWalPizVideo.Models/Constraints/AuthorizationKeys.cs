namespace MorWalPizVideo.Models.Constraints;

public static class AuthorizationPermissionKeys
{
  public const string BackofficeAccess = "backoffice.access";
  public const string BackofficeManageAll = "backoffice.manageall";

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

  public const string ShortLinksView = "shortlinks.view";
  public const string ShortLinksManage = "shortlinks.manage";
  public const string ShortLinksCreate = "shortlinks.create";
  public const string ShortLinksUpdate = "shortlinks.update";
  public const string ShortLinksDelete = "shortlinks.delete";

  public const string FormsView = "forms.view";
  public const string FormsManage = "forms.manage";
  public const string FormsResponsesView = "forms.responses.view";
  public const string InsightsView = "insights.view";
  public const string InsightsManage = "insights.manage";
  public const string InsightsScan = "insights.scan";
  public const string DiagnosticsView = "diagnostics.view";

}

public static class AuthorizationGroupCodes
{
  // Canonical lowercase group codes.
  public const string Admin = "admin";
  public const string Contributor = "contributor";
}
