using Microsoft.Data.SqlClient;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;
using Xunit;
using System;

namespace RentFlowApp.Tests.Infrastructure.Billing;

public class SqlServerBillRepositoryTests
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("RENTFLOW_TEST_CONNECTION")
        ?? @"Server=localhost\SQLEXPRESS;
            Database=RentFlowDb;
            Integrated Security=True;
            TrustServerCertificate=True;";

    [Fact]
    public void Add_WithValidBill_StoresBillInDatabaseAndExistsReturnsTrue()
    {
        // Arrange
        var repository = new SqlServerBillRepository(ConnectionString);
        var roomId = RoomId.New();
        string roomNumber = $"TEST-{Guid.NewGuid():N}";
        InsertTestRoom(roomId, roomNumber);

        var billingPeriod = BillingPeriod.Create("2026-10");
        var calculation = new BillingCalculationResult(
            Money.Create(2800m),
            Money.Create(150m),
            Money.Create(40m),
            Money.Create(2990m));
        var bill = Bill.Create(roomId, billingPeriod, calculation);

        try
        {
            // Act
            repository.Add(bill);

            // Assert
            Assert.True(repository.Exists(roomId, billingPeriod));
        }
        finally
        {
            DeleteTestBill(bill.Id);
            DeleteTestRoom(roomId);
        }
    }

    [Fact]
    public void Add_WithDuplicateRoomAndBillingPeriod_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = new SqlServerBillRepository(ConnectionString);
        var roomId = RoomId.New();
        string roomNumber = $"TEST-{Guid.NewGuid():N}";
        InsertTestRoom(roomId, roomNumber);

        var billingPeriod = BillingPeriod.Create("2026-10");
        var calculation = new BillingCalculationResult(
            Money.Create(2800m),
            Money.Create(150m),
            Money.Create(40m),
            Money.Create(2990m));
        var firstBill = Bill.Create(roomId, billingPeriod, calculation);
        var duplicateBill = Bill.Create(roomId, billingPeriod, calculation);

        try
        {
            repository.Add(firstBill);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                repository.Add(duplicateBill));

            Assert.Contains("มีบิลของห้องและรอบบิลนี้อยู่แล้ว", exception.Message);
        }
        finally
        {
            DeleteTestBill(firstBill.Id);
            DeleteTestBill(duplicateBill.Id);
            DeleteTestRoom(roomId);
        }
    }

    [Fact]
    public void Exists_WhenNoBillForRoomAndPeriod_ReturnsFalse()
    {
        // Arrange
        var repository = new SqlServerBillRepository(ConnectionString);
        var roomId = RoomId.New();
        var billingPeriod = BillingPeriod.Create("2026-10");

        // Act & Assert
        Assert.False(repository.Exists(roomId, billingPeriod));
    }

    private static void InsertTestRoom(RoomId roomId, string roomNumber)
    {
        const string sql = """
            INSERT INTO dbo.Rooms (Id, RoomNumber)
            VALUES (@Id, @RoomNumber);
            """;

        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", roomId.Value);
        command.Parameters.AddWithValue("@RoomNumber", roomNumber);

        connection.Open();
        command.ExecuteNonQuery();
    }

    private static void DeleteTestBill(BillId billId)
    {
        const string sql = """
            DELETE FROM dbo.Bills
            WHERE Id = @Id;
            """;

        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", billId.Value);

        connection.Open();
        command.ExecuteNonQuery();
    }

    private static void DeleteTestRoom(RoomId roomId)
    {
        const string sql = """
            DELETE FROM dbo.Rooms
            WHERE Id = @Id;
            """;

        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", roomId.Value);

        connection.Open();
        command.ExecuteNonQuery();
    }
}
