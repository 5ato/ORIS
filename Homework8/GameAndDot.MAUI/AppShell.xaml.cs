namespace GameAndDot.MAUI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(UserInputPage), typeof(UserInputPage));
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        }
    }
}
