using HomeNotes.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HomeNotes.Desktop.ViewModels
{
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly AuthClientService _authClientService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly ChooseStorageViewModel _chooseStorageViewModel;
        [ObservableProperty] private string _login;
        [ObservableProperty] private string _password;
        [ObservableProperty]
        private bool _isMode = false;
        private readonly Func<int, NotesWriteViewModel> _notesWriteViewModelFactory;
        private string _path;
        public RegisterViewModel(AuthClientService authClientService, MainWindowViewModel mainWindowViewModel, Func<int, NotesWriteViewModel> notesWriteViewModelFactory)
        
        {
            _authClientService = authClientService;
            _mainWindowViewModel = mainWindowViewModel;
            _notesWriteViewModelFactory = notesWriteViewModelFactory;
          
        }

        [RelayCommand]
        public Task Register()
        {
           
             if (string.IsNullOrEmpty(_login) || string.IsNullOrEmpty(_password))
             {
                 return null;
             }
             var signUp =_authClientService.Register(_login, _password);
             if (signUp != null)
             {
                 if (_path != null)
                 {
                     _mainWindowViewModel.CurrentViewModel = _notesWriteViewModelFactory(signUp.Id);
                 }
                 else
                 {
                     _mainWindowViewModel.CurrentViewModel = _chooseStorageViewModel;
                 }
   
             }
             return Task.CompletedTask;
        }
       
        [RelayCommand]
        void GoToSignUp() => IsMode= true;
        [RelayCommand]
        void GoToSignIn() => IsMode = false;

    }
}
