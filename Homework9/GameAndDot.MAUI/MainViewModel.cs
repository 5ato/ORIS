using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GameAndDot.MAUI;

/// <summary>
/// ViewModel контролирует логику основного экрана приложения
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private UserInfo _currentUser;

    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler CanvasChanged;

    private ClientProcessor _processor;

    /// <summary>
    /// Текущий игрок
    /// </summary>
    public UserInfo CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Коллекция других игроков
    /// </summary>
    public ObservableCollection<UserInfo> OtherPlayers { get; set; }

    /// <summary>
    /// Сервис для рисования точек на канвасе
    /// </summary>
    public DotCanvasDrawable DrawableCanvas { get; set; }

    public MainViewModel(string currentPlayer)
    {
        CurrentUser = new UserInfo
        {
            Id = Guid.NewGuid().ToString(),
            Username = currentPlayer,
            DotColor = Colors.Black
        };

        DrawableCanvas = new DotCanvasDrawable();

        OtherPlayers = new ObservableCollection<UserInfo>();
        
        _processor = new ClientProcessor(CurrentUser.Id);

        _ = _processor.ConnectClient(CurrentUser, EventMessageHandler);

        CurrentUser.DotColorChanged += (s, e) =>
        {
            _ =_processor.SendMessageAsync(new EventMessage()
            {
                Type = EventType.PlayerSwitchColor,
                Id = CurrentUser.Id,
                Username = CurrentUser.Username,
                Color = CurrentUser.DotColor.ToInt()
            });
        };
    }

    private void EventMessageHandler(EventMessage messageRequest)
    {
        switch (messageRequest?.Type)
        {
            case EventType.PlayerConnected:
                SynchronizePlayers(messageRequest);

                break;

            case EventType.PlayerDisconnected:
                SynchronizePlayers(messageRequest);

                break;
            case EventType.PlayerDraw:
                DrawableCanvas.AddDot(new Point(messageRequest.X, messageRequest.Y), Color.FromInt(messageRequest.Color));
                CanvasChanged?.Invoke(this, EventArgs.Empty);
                break;
            case EventType.PlayerSwitchColor:
                var playerToUpdate = OtherPlayers.FirstOrDefault(p => p.Id == messageRequest.Id);
                if (playerToUpdate != null)
                {
                    playerToUpdate.DotColor = Color.FromInt(messageRequest.Color);
                }
                break;
        }
    }

    /// <summary>
    /// Добавляем точку в нужную позицию с указанным цветом
    /// </summary>
    public async Task AddDot(Point position, Color color)
    {
        DrawableCanvas.AddDot(position, color);

        await _processor.SendMessageAsync(new EventMessage()
        {
            Type = EventType.PlayerDraw,
            Id = CurrentUser.Id,
            Username = CurrentUser.Username,
            Color = color.ToInt(),
            X = position.X,
            Y = position.Y
        });
    }

    private void SynchronizePlayers(EventMessage message)
    {
        var playersFromServer = message.Players;

        // Удаляем тех, кого нет в новом списке
        var idsFromServer = playersFromServer.Select(p => p.Id).ToHashSet();

        for (int i = OtherPlayers.Count - 1; i >= 0; i--)
        {
            if (!idsFromServer.Contains(OtherPlayers[i].Id))
            {
                OtherPlayers.RemoveAt(i);
            }
        }

        // Добавляем или обновляем существующих
        foreach (var serverPlayer in playersFromServer)
        {
            if (serverPlayer.Id == CurrentUser.Id) continue; // Пропускаем себя

            var existingPlayer = OtherPlayers.FirstOrDefault(p => p.Id == serverPlayer.Id);

            if (existingPlayer == null)
            {
                // Добавляем нового
                OtherPlayers.Add(new UserInfo
                {
                    Id = serverPlayer.Id,
                    Username = serverPlayer.Username,
                    DotColor = Color.FromInt(serverPlayer.Color)
                });
            }
            else
            {
                // Обновляем данные существующего
                existingPlayer.Username = serverPlayer.Username;
                existingPlayer.DotColor = Color.FromInt(serverPlayer.Color);
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _ = _processor.DisconnectAsync();
    }
}
