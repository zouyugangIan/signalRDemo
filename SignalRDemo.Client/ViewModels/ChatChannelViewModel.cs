using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using SignalRDemo.Shared.DTOs;

namespace SignalRDemo.Client.ViewModels;

public enum ChannelType
{
    Global,
    Room,
    Private
}

public partial class ChatChannelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private ChannelType _type;
    
    [ObservableProperty]
    private bool _hasUnreadMessages;

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();

    public ChatChannelViewModel(string id, string displayName, ChannelType type)
    {
        Id = id;
        DisplayName = displayName;
        Type = type;
        Id = id; // Set again to ensure property change, though constructor sets field directly usually.
    }

    public void AddMessage(ChatMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Messages.Add(new ChatMessageViewModel(message));
            // Should scroll to bottom logic here if needed
        });
    }
}
