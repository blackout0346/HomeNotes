using HomeNotes.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Desktop.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly AuthClientService _authClientService;


        public RegisterViewModel(AuthClientService authClientService)
        {
            _authClientService = authClientService;


            TestRegistration();
        }

        public void TestRegistration()
        {

            _authClientService.Register("TEST2", "qtgbedg");
        }
    }
}
