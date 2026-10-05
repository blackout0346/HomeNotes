using HomeNotes.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNotes.Desktop.Interface;

namespace HomeNotes.Desktop.ViewModels
{
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly AuthClientService _authClientService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly ChooseStorageViewModel _chooseStorageViewModel;
        private readonly Func<Guid?, NotesWriteViewModel> _notesWriteViewModelFactory;
        private readonly IStorage _storage;
        [ObservableProperty] private string _login;
        [ObservableProperty] private string _password;
        [ObservableProperty] private bool _isMode = false;


        public RegisterViewModel(IStorage storage, ChooseStorageViewModel chooseStorageViewModel,
            AuthClientService authClientService, MainWindowViewModel mainWindowViewModel,
            Func<Guid?, NotesWriteViewModel> notesWriteViewModelFactory)
        {
            _storage = storage;
            _chooseStorageViewModel = chooseStorageViewModel;
            _authClientService = authClientService;
            _mainWindowViewModel = mainWindowViewModel;
            _notesWriteViewModelFactory = notesWriteViewModelFactory;
        }

        [RelayCommand]
        public async Task SignUp()
        {
            var signUp = await _authClientService.Register(_login, _password);
            if (signUp != null)
            {
                NavigateToNotes(signUp.UserId);
            }
        }

        [RelayCommand]
        public async Task SignIn()
        {
            var signIn = await _authClientService.Login(_login, _password);
            if (signIn != null)
            {
                NavigateToNotes(signIn.UserId);
            }
        }

        [RelayCommand]
        public void GoToSignUp() => IsMode = true;

        [RelayCommand]
        public void GoToSignIn() => IsMode = false;

        [RelayCommand]
        public void GoToNoAccount()
        {
            NavigateToNotes(null);
        }

        private void NavigateToNotes(Guid? userId)
        {System.Diagnostics.Debug.WriteLine($"Path: '{_storage.StoragePath}'");
            
            if (!string.IsNullOrEmpty(_storage.StoragePath))
            {
                _mainWindowViewModel.CurrentViewModel = _notesWriteViewModelFactory(userId);
            }
            else
            {
                void CleanUp()
                {
                    _chooseStorageViewModel.StorageReady -= OnStorageReady;
                    _chooseStorageViewModel.BackRequested -= OnBackRequested;
                }

                void OnStorageReady()
                {
                    CleanUp();
                    _mainWindowViewModel.CurrentViewModel = _notesWriteViewModelFactory(userId);
                }

                void OnBackRequested()
                {
                    CleanUp();

                    _mainWindowViewModel.CurrentViewModel = this;
                }

                _chooseStorageViewModel.StorageReady += OnStorageReady;
                _chooseStorageViewModel.BackRequested += OnBackRequested;
                _mainWindowViewModel.CurrentViewModel = _chooseStorageViewModel;
            }
        }
    }
}