using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace MorWalPiz.Contracts.Contracts;

[DataContract]
public class RbacUserSummaryContract
{
  [DataMember]
  public string Id { get; set; } = string.Empty;

  [DataMember]
  public string Username { get; set; } = string.Empty;

  [DataMember]
  public string Email { get; set; } = string.Empty;

  [DataMember]
  public bool IsActive { get; set; }

  [DataMember]
  public DateTime? LastLogin { get; set; }

  [DataMember]
  public List<string> GroupIds { get; set; } = new();

  [DataMember]
  public List<string> GroupCodes { get; set; } = new();

  [DataMember]
  public List<string> DirectPermissions { get; set; } = new();

  [DataMember]
  public List<string> EffectivePermissions { get; set; } = new();

  [DataMember]
  public bool CanAccessBackoffice { get; set; }
}

[DataContract]
public class RbacGroupContract
{
  [DataMember]
  public string Id { get; set; } = string.Empty;

  [DataMember]
  public string Code { get; set; } = string.Empty;

  [DataMember]
  public string Name { get; set; } = string.Empty;

  [DataMember]
  public string Description { get; set; } = string.Empty;

  [DataMember]
  public bool IsActive { get; set; }

  [DataMember]
  public List<string> Permissions { get; set; } = new();

  [DataMember]
  public int MemberCount { get; set; }
}

[DataContract]
public class UpsertRbacGroupRequestContract
{
  [DataMember]
  [Required]
  public string Code { get; set; } = string.Empty;

  [DataMember]
  [Required]
  public string Name { get; set; } = string.Empty;

  [DataMember]
  public string Description { get; set; } = string.Empty;

  [DataMember]
  public bool IsActive { get; set; } = true;

  [DataMember]
  public List<string> Permissions { get; set; } = new();
}

[DataContract]
public class UpdateUserDirectPermissionsRequestContract
{
  [DataMember]
  public List<string> Permissions { get; set; } = new();
}

[DataContract]
public class UpdateUserGroupMembershipsRequestContract
{
  [DataMember]
  public List<string> GroupIds { get; set; } = new();
}
