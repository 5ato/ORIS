namespace GameAndDot.MAUI;

[QueryProperty(nameof(Username), "Username")]
public partial class MainPage : ContentPage
{
    private MainViewModel _viewModel;

    private string _username;
    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            _viewModel = new MainViewModel(_username);
            _viewModel.CanvasChanged += (s, e) =>
            {
                CanvasView.Invalidate();
            };
            BindingContext = _viewModel;
        }
    }
    public MainPage()
    {
        InitializeComponent();

    }

    /// <summary>
    /// Хендлер для обработки нажатий на канвас
    /// </summary>
    private async void OnCanvasTapped(object sender, TappedEventArgs e)
    {
        // Получаем позицию нажатия
        var tapPoint = e.GetPosition((View)sender);

        if (tapPoint.HasValue)
        {
            // Добавляем новую точку
            await _viewModel.AddDot(tapPoint.Value, _viewModel.CurrentUser.DotColor);

            // Обновляем состояния канваса
            CanvasView.Invalidate();
        }
    }

    /// <summary>
    /// Открывает диалог-меню для выбора цвета
    /// </summary>
    private async void OnSelectColorClicked(object sender, EventArgs e)
    {
        string[] colorOptions =
        [
            "Black", "Red", "Blue", "Green", "Yellow",
            "Orange", "Purple", "Pink", "Brown", "Cyan"
        ];

        string selectedColor = await DisplayActionSheet(
            "Select Your Dot Color",
            "Cancel",
            null,
            colorOptions);

        if (!string.IsNullOrEmpty(selectedColor) && selectedColor != "Cancel")
        {
            _viewModel.CurrentUser.DotColor = GetColorFromName(selectedColor);
        }
    }

    /// <summary>
    /// Преобразует из строки в цвет
    /// </summary>
    private static Color GetColorFromName(string colorName)
    {
        return colorName switch
        {
            "Black" => Colors.Black,
            "Red" => Colors.Red,
            "Blue" => Colors.Blue,
            "Green" => Colors.Green,
            "Yellow" => Colors.Yellow,
            "Orange" => Colors.Orange,
            "Purple" => Colors.Purple,
            "Pink" => Colors.Pink,
            "Brown" => Colors.Brown,
            "Cyan" => Colors.Cyan,
            _ => Colors.Black
        };
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Dispose();
    }
}
