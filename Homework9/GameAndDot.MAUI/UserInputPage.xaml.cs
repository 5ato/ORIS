using System.Threading.Tasks;

namespace GameAndDot.MAUI;

public partial class UserInputPage : ContentPage
{
	public UserInputPage()
	{
		InitializeComponent();
	}

    private async void GoToGame(object sender, EventArgs e)
    {
		var username = (string)UsernameInput.GetValue(Entry.TextProperty);
        if (!string.IsNullOrEmpty(username))
		{
			await Shell.Current.GoToAsync($"{nameof(MainPage)}?Username={username}");
		}
    }
}