using PermissionsApi.TestSupport;
using Xunit;

namespace PermissionsAPI.ReqNRoll;

[CollectionDefinition("MySQL")]
public class MySqlCollectionFixture : ICollectionFixture<MySqlTestFixture>
{
}
