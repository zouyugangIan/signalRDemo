namespace SignalRDemo.Shared.DTOs;

public class RoomInfo
{
    public string RoomName { get; set; } = string.Empty;
    public int UserCount { get; set; }

    public RoomInfo() { }

    public RoomInfo(string roomName, int userCount)
    {
        RoomName = roomName;
        UserCount = userCount;
    }
}
