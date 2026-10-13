
using Microsoft.Data.SqlClient;
using RentFlowApp;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Rooms;
using Xunit;
using System;

namespace RentFlowApp.Tests.Infrastructure.Rooms;

public class SqlServerRoomRepositoryTests
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("RENTFLOW_TEST_CONNECTION")
        ?? @"Server=localhost\SQLEXPRESS;
            Database=RentFlowDb;
            Integrated Security=True;
            TrustServerCertificate=True;";

    [Fact]
    public void Add_WithValidRoom_StoresRoomInDatabase()
    {
        // Arrange
        string roomNumber = $"TEST-{Guid.NewGuid():N}";
        var repository = new SqlServerRoomRepository(ConnectionString);

        var room = new Room(
            RoomId.New(),
            new RoomNumber(roomNumber)
        );

        try
        {
            // Act
            repository.Add(room);

            // Assert
            var rooms = repository.GetAll();

            Assert.Contains(
                rooms,
                savedRoom => savedRoom.Id == room.Id
                    && savedRoom.RoomNumber.Value == roomNumber
            );
        }
        finally
        {
            DeleteTestRoom(roomNumber);
        }
    }

    [Fact]
    public void Add_WithDuplicateRoomNumber_ThrowsException()
    {
        // Arrange
        string roomNumber = $"TEST-{Guid.NewGuid():N}";
        var repository = new SqlServerRoomRepository(ConnectionString);

        var firstRoom = new Room(
            RoomId.New(),
            new RoomNumber(roomNumber)
        );

        var duplicateRoom = new Room(
            RoomId.New(),
            new RoomNumber(roomNumber)
        );

        try
        {
            repository.Add(firstRoom);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => repository.Add(duplicateRoom)
            );
        }
        finally
        {
            DeleteTestRoom(roomNumber);
        }
    }

    private static void DeleteTestRoom(string roomNumber)
    {
        const string sql = """
            DELETE FROM dbo.Rooms
            WHERE RoomNumber = @RoomNumber;
            """;

        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@RoomNumber", roomNumber);

        connection.Open();
        command.ExecuteNonQuery();
    }


    [Fact]
    public void GetAll_WithSavedRoom_ReturnsRoomFromDatabase()
    {
        // Arrange
        string roomNumber = $"TEST-{Guid.NewGuid():N}";
        var repository = new SqlServerRoomRepository(ConnectionString);

        var room = new Room(
            RoomId.New(),
            new RoomNumber(roomNumber)
        );

        try
        {
            repository.Add(room);

            // Act
            var rooms = repository.GetAll();

            // Assert
            var savedRoom = Assert.Single(
                rooms,
                item => item.RoomNumber.Value == roomNumber
            );

            Assert.Equal(room.Id, savedRoom.Id);
        }
        finally
        {
            DeleteTestRoom(roomNumber);
        }
    }


}
