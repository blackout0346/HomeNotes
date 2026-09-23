using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HomeNotes.Desktop.Services;
using HomeNotes.Desktop.ViewModels;
using HomeNotes.Desktop.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace HomeNotes.Desktop
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; }
        public IConfiguration Configuration { get; }
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public App()
        {
            var builder = new ConfigurationBuilder()
         .SetBasePath(AppContext.BaseDirectory) 
         .AddJsonFile("Application.json", optional: false, reloadOnChange: true); 
            Configuration = builder.Build();
            Services = ConfigureServices(Configuration);
           
        }
        private static IServiceProvider ConfigureServices(IConfiguration configuration)
        {
            var baseurl = configuration["ApiSettings:BaseUrl"];
            var services = new ServiceCollection();
            services.AddHttpClient("HomeNotesApi",client =>
            {
                client.BaseAddress = new Uri(baseurl); 
            });
            services.AddHttpClient<AuthClientService>("HomeNotesApi");
            services.AddHttpClient<NoteClientService>("HomeNotesApi");
            services.AddHttpClient<AttachmentClientService>("HomeNotesApi");
            services.AddHttpClient<SyncClientService>("HomeNotesApi");
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<NotesWriteViewModel>();
            services.AddSingleton<Func<int, NotesWriteViewModel>>(sp => id =>
                ActivatorUtilities.CreateInstance<NotesWriteViewModel>(sp, id));
            return services.BuildServiceProvider();

        }
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
         
                var currentpage = Services.GetRequiredService<MainWindowViewModel>();
                var register = Services.GetRequiredService<RegisterViewModel>();
                currentpage.CurrentViewModel = register;
                desktop.MainWindow = new MainWindow
                {
                    DataContext = currentpage
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}