using System.Data;
using Microsoft.Data.SqlClient;
using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Billing;

public sealed class SqlServerBillRepository : IBillRepository
{
    private readonly string _connectionString;

    public SqlServerBillRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public void Add(Bill bill)
    {
        ArgumentNullException.ThrowIfNull(bill);

        const string sql = """
            INSERT INTO dbo.Bills (
                Id,
                RoomId,
                BillingPeriod,
                RentAmount,
                WaterAmount,
                ElectricityAmount,
                TotalAmount
            )
            VALUES (
                @Id,
                @RoomId,
                @BillingPeriod,
                @RentAmount,
                @WaterAmount,
                @ElectricityAmount,
                @TotalAmount
            );
            """;

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                .Value = bill.Id.Value;
            command.Parameters.Add("@RoomId", SqlDbType.UniqueIdentifier)
                .Value = bill.RoomId.Value;
            command.Parameters.Add("@BillingPeriod", SqlDbType.NVarChar, 7)
                .Value = bill.BillingPeriod.Value;
            command.Parameters.Add("@RentAmount", SqlDbType.Decimal)
                .Value = bill.Rent.Amount;
            command.Parameters.Add("@WaterAmount", SqlDbType.Decimal)
                .Value = bill.Water.Amount;
            command.Parameters.Add("@ElectricityAmount", SqlDbType.Decimal)
                .Value = bill.Electricity.Amount;
            command.Parameters.Add("@TotalAmount", SqlDbType.Decimal)
                .Value = bill.Total.Amount;

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("มีบิลของห้องและรอบบิลนี้อยู่แล้ว", ex);
        }
    }

    public bool Exists(RoomId roomId, BillingPeriod billingPeriod)
    {
        ArgumentNullException.ThrowIfNull(roomId);
        ArgumentNullException.ThrowIfNull(billingPeriod);

        const string sql = """
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM dbo.Bills
                    WHERE RoomId = @RoomId AND BillingPeriod = @BillingPeriod
                )
                THEN 1
                ELSE 0
            END;
            """;

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@RoomId", SqlDbType.UniqueIdentifier)
            .Value = roomId.Value;
        command.Parameters.Add("@BillingPeriod", SqlDbType.NVarChar, 7)
            .Value = billingPeriod.Value;

        connection.Open();

        return (int)command.ExecuteScalar()! == 1;
    }
}
