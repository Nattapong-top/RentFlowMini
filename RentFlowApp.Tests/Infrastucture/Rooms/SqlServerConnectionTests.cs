
using Microsoft.Data.SqlClient;
using Xunit;

namespace RentFlowApp.Tests.Infrastructure.Rooms;

public class SqlServerConnectionTests
{
    [Fact]
    public void Open_WithValidConnectionString_ConnectsSuccessfully()
    {
        const string connectionString =
            @"Server=NATTAPONG_PC_i5\SQLEXPRESS;Database=RentFlowDb;Integrated Security=True;TrustServerCertificate=True;";

        using var connection = new SqlConnection(connectionString);

        connection.Open();

        Assert.Equal(
            System.Data.ConnectionState.Open,
            connection.State
        );
    }
}
