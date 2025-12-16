using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameAndDot.MAUI;

/// <summary>
/// Model representing user information for the canvas application
/// Implements INotifyPropertyChanged for data binding support
/// </summary>
public class UserInfo : INotifyPropertyChanged
{
    private string _id;
    private string _username;
    private Color _dotColor;

    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler DotColorChanged;

    public string Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// The user's display name
    /// </summary>
    public string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// The color of dots this user places on the canvas
    /// </summary>
    public Color DotColor
    {
        get => _dotColor;
        set
        {
            if (_dotColor != value)
            {
                _dotColor = value;
                DotColorChanged?.Invoke(this, EventArgs.Empty);
                OnPropertyChanged();
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
