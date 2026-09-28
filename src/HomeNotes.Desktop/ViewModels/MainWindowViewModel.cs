using CommunityToolkit.Mvvm.ComponentModel;
using HomeNotes.Desktop.Services;

namespace HomeNotes.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty] public object? currentViewModel;
    
    }
}
