namespace RentFlowApp.Infrastructure.Sqlite;

public sealed record SchemaUpdate(long Version, string Sql);
