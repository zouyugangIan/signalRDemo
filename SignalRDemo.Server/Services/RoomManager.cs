using System.Collections.Concurrent;
using SignalRDemo.Shared.DTOs;

namespace SignalRDemo.Server.Services;

public class RoomManager
{
    // RoomName -> RoomInfo
    private readonly ConcurrentDictionary<string, RoomInfo> _rooms = new();
    
    // RoomName -> Set of ConnectionIds
    private readonly ConcurrentDictionary<string, HashSet<string>> _roomUsers = new();

    // ConnectionId -> Set of RoomNames (for quick lookup on disconnect)
    private readonly ConcurrentDictionary<string, HashSet<string>> _userRooms = new();

    public RoomManager()
    {
        // Add some default rooms
        CreateRoom("General");
        CreateRoom("Random");
        CreateRoom("Help");
    }

    public List<RoomInfo> GetRooms()
    {
        return _rooms.Values.OrderByDescending(r => r.UserCount).ThenBy(r => r.RoomName).ToList();
    }

    public RoomInfo CreateRoom(string roomName)
    {
        return _rooms.GetOrAdd(roomName, name => new RoomInfo(name, 0));
    }

    public RoomInfo? AddUserToRoom(string roomName, string connectionId)
    {
        var room = CreateRoom(roomName); // Ensure room exists

        _roomUsers.AddOrUpdate(roomName, 
            new HashSet<string> { connectionId }, 
            (key, set) => {
                lock(set) { set.Add(connectionId); }
                return set;
            });

        _userRooms.AddOrUpdate(connectionId,
            new HashSet<string> { roomName },
            (key, set) => {
                lock(set) { set.Add(roomName); }
                return set;
            });

        UpdateRoomCount(roomName);
        return _rooms.GetValueOrDefault(roomName);
    }

    public void RemoveUserFromRoom(string roomName, string connectionId)
    {
        if (_roomUsers.TryGetValue(roomName, out var users))
        {
            lock (users)
            {
                users.Remove(connectionId);
            }
            UpdateRoomCount(roomName);

            // Optional: Remove empty rooms if they are not default ones
            // if (users.Count == 0 && !IsDefaultRoom(roomName)) ...
        }

        if (_userRooms.TryGetValue(connectionId, out var rooms))
        {
            lock (rooms)
            {
                rooms.Remove(roomName);
            }
        }
    }

    public List<string> RemoveUserFromAllRooms(string connectionId)
    {
        var leftRooms = new List<string>();
        if (_userRooms.TryRemove(connectionId, out var rooms))
        {
            foreach (var roomName in rooms)
            {
                if (_roomUsers.TryGetValue(roomName, out var users))
                {
                    lock (users)
                    {
                        users.Remove(connectionId);
                    }
                    UpdateRoomCount(roomName);
                    leftRooms.Add(roomName);
                }
            }
        }
        return leftRooms;
    }

    private void UpdateRoomCount(string roomName)
    {
        if (_roomUsers.TryGetValue(roomName, out var users) && _rooms.TryGetValue(roomName, out var room))
        {
            room.UserCount = users.Count;
        }
    }
}
