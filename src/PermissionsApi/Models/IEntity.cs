using System.Text.Json.Serialization;
using PermissionsApi.Services;

namespace PermissionsApi.Models;

[JsonDerivedType(typeof(Permission), typeDiscriminator: "permission")]
[JsonDerivedType(typeof(Group), typeDiscriminator: "group")]
[JsonDerivedType(typeof(User), typeDiscriminator: "user")]
[JsonDerivedType(typeof(EmptyEntity), typeDiscriminator: "empty")]
public interface IEntity
{
    string Id { get; }
}
