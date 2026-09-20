using HomeNotes.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HomeNotes.Desktop.ViewModels
{
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly AuthClientService _authClientService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        [ObservableProperty]
        private bool _isMode = false;

        public RegisterViewModel(AuthClientService authClientService, MainWindowViewModel mainWindowViewModel)
        {
            _authClientService = authClientService;
            _mainWindowViewModel = mainWindowViewModel;

            TestRegistration();
        }

        public void TestRegistration()
        {

            _authClientService.Register("TEST2", "qtgbedg");
        }
        [RelayCommand]
        void GoToSignUp() => IsMode= true;
        [RelayCommand]
        void GoToSignIn() => IsMode = false;

    }
}
