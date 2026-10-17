
using RentFlowApp;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Rooms;
using Xunit;

namespace RentFlowApp.Tests.Infrastructure.Rooms;

public class InMemoryRoomRepositoryTests
{
    [Fact]
    public void GetAll_WithNoRooms_ReturnsEmptyList()
    {
        // Arrange
        var repository = new InMemoryRoomRepository();

        // Act
        var rooms = repository.GetAll();

        // Assert
        Assert.Empty(rooms);
    }

    [Fact]
    public void GetAll_WithExistingRooms_ReturnsAllRooms()
    {
        // Arrange
        var repository = new InMemoryRoomRepository();

        var room101 = new Room(
            RoomId.New(),
            new RoomNumber("101")
        );

        var room102 = new Room(
            RoomId.New(),
            new RoomNumber("102")
        );

        repository.Add(room101);
        repository.Add(room102);

        // Act
        var rooms = repository.GetAll();

        // Assert
        Assert.Equal(2, rooms.Count);
        Assert.Contains(room101, rooms);
        Assert.Contains(room102, rooms);
    }

    [Fact]
    public void Add_WithValidRoom_StoresRoom()
    {
        // Arrange
        var repository = new InMemoryRoomRepository();

        var room = new Room(
            RoomId.New(),
            new RoomNumber("101")
        );

        // Act
        repository.Add(room);

        // Assert
        var rooms = repository.GetAll();

        Assert.Single(rooms);
        Assert.Contains(room, rooms);
    }

    [Fact]
    public void ExistsByRoomNumber_WithExistingRoom_ReturnsTrue()
    {
        // Arrange
        var repository = new InMemoryRoomRepository();

        var room = new Room(
            RoomId.New(),
            new RoomNumber("101")
        );

        repository.Add(room);

        // Act
        bool exists = repository.ExistsByRoomNumber("101");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void ExistsByRoomNumber_WithUnknownRoom_ReturnsFalse()
    {
        // Arrange
        var repository = new InMemoryRoomRepository();

        // Act
        bool exists = repository.ExistsByRoomNumber("999");

        // Assert
        Assert.False(exists);
    }


    [Fact]
    public void Add_WithDuplicateRoomNumber_ThrowsException()
    {
        // Arrange
        var repository = new InMemoryRoomRepository();

        var firstRoom = new Room(
            RoomId.New(),
            new RoomNumber("101")
        );

        var duplicateRoom = new Room(
            RoomId.New(),
            new RoomNumber("101")
        );

        repository.Add(firstRoom);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => repository.Add(duplicateRoom)
        );
    }

    [Fact]
    public void Add_WithDuplicateRoomId_ThrowsException()
    {
        var repository = new InMemoryRoomRepository();
        var roomId = RoomId.New();

        repository.Add(new Room(roomId, new RoomNumber("101")));

        Assert.Throws<InvalidOperationException>(() =>
            repository.Add(new Room(roomId, new RoomNumber("102"))));
    }

    [Fact]
    public void GetAll_ReturnsRoomsOrderedByRoomNumber()
    {
        var repository = new InMemoryRoomRepository();
        var room102 = new Room(RoomId.New(), new RoomNumber("102"));
        var room101 = new Room(RoomId.New(), new RoomNumber("101"));

        repository.Add(room102);
        repository.Add(room101);

        var rooms = repository.GetAll();

        Assert.Equal("101", rooms[0].RoomNumber.Value);
        Assert.Equal("102", rooms[1].RoomNumber.Value);
    }

    [Fact]
    public void GetAll_ReturnsRoomsWithPersistedValues()
    {
        var repository = new InMemoryRoomRepository();
        var room = new Room(RoomId.New(), new RoomNumber("101"));
        repository.Add(room);

        var savedRoom = Assert.Single(repository.GetAll());

        Assert.Equal(room.Id, savedRoom.Id);
        Assert.Equal(room.RoomNumber.Value, savedRoom.RoomNumber.Value);
        Assert.NotSame(room, savedRoom);
    }


}
