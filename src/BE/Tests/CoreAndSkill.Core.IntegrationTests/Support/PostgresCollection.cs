using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// Mọi test class cần PostgreSQL thật khai [Collection(PostgresCollection.Name)] — dùng chung MỘT
// container (PostgresFixture) và chạy tuần tự với nhau.
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
