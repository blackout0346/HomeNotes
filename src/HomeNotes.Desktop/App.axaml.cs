using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HomeNotes.Desktop.Services;
using HomeNotes.Desktop.ViewModels;
using HomeNotes.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace HomeNotes.Desktop
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; }
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public App()
        {
            Services = ConfigureServices();
        }
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddHttpClient<AuthClientService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:5335/"); 
            });


            services.AddTransient<MainWindowViewModel>();
            return services.BuildServiceProvider();

        }
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var main = Services.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext =main,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}