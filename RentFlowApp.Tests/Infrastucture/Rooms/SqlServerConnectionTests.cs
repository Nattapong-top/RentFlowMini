
using Microsoft.Data.SqlClient;
using Xunit;

namespace RentFlowApp.Tests.Infrastructure.Rooms;

public class SqlServerConnectionTests
{
    [Fact]
    public void Open_WithValidConnectionString_ConnectsSuccessfully()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("RENTFLOW_TEST_CONNECTION")
            ?? @"Server=localhost\SQLEXPRESS;
                Database=RentFlowDb;
                Integrated Security=True;
                TrustServerCertificate=True;";

        using var connection = new SqlConnection(connectionString);

        connection.Open();

        Assert.Equal(
            System.Data.ConnectionState.Open,
            connection.State
        );
    }
}
