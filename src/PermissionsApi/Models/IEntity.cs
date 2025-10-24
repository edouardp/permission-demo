using System.Text.Json.Serialization;

namespace PermissionsApi.Models;

[JsonDerivedType(typeof(Permission), typeDiscriminator: "permission")]
[JsonDerivedType(typeof(Group), typeDiscriminator: "group")]
[JsonDerivedType(typeof(User), typeDiscriminator: "user")]
public interface IEntity
{
    string Id { get; }
}
