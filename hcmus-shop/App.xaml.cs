using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Reflection;
using hcmus_shop.Data;
using hcmus_shop.ViewModels;
using hcmus_shop.Views;
using hcmus_shop.Services.Auth;


namespace hcmus_shop
{
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; } = null!;
        public Window? CurrentWindow { get; private set; }
        private Window? _loginWindow;
        private Window? _mainWindow;

        public App()
        {
            InitializeComponent();

            //// === SETUP DEPENDENCY INJECTION + MVVM TOOLKIT ===
            //Ioc.Default.ConfigureServices(
            //    new ServiceCollection()
            //        // Đăng ký các Service và ViewModel ở đây sau
            //        // Ví dụ:
            //        // .AddSingleton<ILoginService, LoginService>()
            //        // .AddTransient<LoginViewModel>()
            //        // .AddTransient<DashboardViewModel>()
            //        .BuildServiceProvider());

            // === SETUP CONFIGURATION ===
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var exePath = Assembly.GetExecutingAssembly().Location;
            var appDirectory = Path.GetDirectoryName(exePath);

            var builder = new ConfigurationBuilder()
                .SetBasePath(appDirectory!)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            // === SETUP DEPENDENCY INJECTION ===
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddDbContextFactory<MyShopDbContext>(options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
                }

                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.EnableRetryOnFailure(maxRetryCount: 5);
                })
                .UseSnakeCaseNamingConvention();
            });

            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
            services.AddTransient<LoginViewModel>();

            Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _loginWindow = new LoginWindow();
            CurrentWindow = _loginWindow;
            _loginWindow.Activate();
        }

        public void OpenMainWindow()
        {
            _mainWindow ??= new MainWindow();
            CurrentWindow = _mainWindow;
            _mainWindow.Activate();
            _loginWindow?.Close();
            _loginWindow = null;
        }
    }
}
