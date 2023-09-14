using System;
using System.Threading;
using System.Windows;
using CSWPF.Core;
using CSWPF.MVVM.Model;
using CSWPF.MVVM.Model.Interface;
using CSWPF.MVVM.View;
using CSWPF.MVVM.ViewModel;
using CSWPF.Services;
using CSWPF.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace CSWPF
{
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;
        public App()
        {
            IServiceCollection services = new ServiceCollection();
            
            services.AddTransient<MainWindow>(provider => new MainWindow
            {
                DataContext = provider.GetRequiredService<MainViewModel>()
            });

            services.AddTransient<IUserService, UserService>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<SettingViewModel>();
            services.AddTransient<AddViewModel>();
            services.AddTransient<INavigationService, NavigationService>();
            

            services.AddTransient<Func<Type, ViewModel>>(serviceProvider => viewModelType => (ViewModel)serviceProvider.GetRequiredService(viewModelType));

            _serviceProvider = services.BuildServiceProvider();
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            var homeViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            mainViewModel.Navigation.CurrentView = homeViewModel;
            
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            
            base.OnStartup(e);
        }
    }
}