using HomeNotes.Desktop.Services;

namespace HomeNotes.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string Greeting { get; } = "Welcome to Avalonia!";
        public AuthClientService _authClientService;
        public MainWindowViewModel(AuthClientService authClientService )
        {
            _authClientService = authClientService;
            //_authClientService.Register("testuser", "password123").Wait();
            _authClientService.Login("testuser", "password123").Wait();


        }

    }
}
