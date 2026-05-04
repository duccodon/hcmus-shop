using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.WindowsAppRuntime;   
using System;
using System.IO;
using System.Reflection;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Services.GraphQL;
using hcmus_shop.Services.Auth;
using hcmus_shop.Services.Brands;
using hcmus_shop.Services.Categories;
using hcmus_shop.Services.Series;
using hcmus_shop.Services.Products;
using hcmus_shop.Services.Config;
using hcmus_shop.Services.Dashboard;
using hcmus_shop.ViewModels;
using hcmus_shop.ViewModels.Auth;
using hcmus_shop.ViewModels.Products;
using hcmus_shop.Views;
using System.Threading.Tasks;
using Windows.Storage;

namespace hcmus_shop
{
    public partial class App : Application
    {
        private const string ConfigServerUrlKey = "config_server_url";

        public static IConfiguration Configuration { get; private set; } = null!;
        public Window? CurrentWindow { get; private set; }
        private Window? _loginWindow;
        private Window? _mainWindow;

        public App()
        {
            InitializeComponent();

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

            // GraphQL Client — use saved config URL or fall back to appsettings default
            var serverUrl = GetConfiguredServerUrl();
            services.AddSingleton<IGraphQLClientService>(new GraphQLClientService(serverUrl));

            // Config (pre-login server URL)
            services.AddSingleton<IConfigService, ConfigService>();

            // Auth
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

            // Feature Services (replace old Repositories)
            services.AddSingleton<IBrandService, BrandService>();
            services.AddSingleton<ICategoryService, CategoryService>();
            services.AddSingleton<ISeriesService, SeriesService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<IDashboardService, DashboardService>();

            // ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<ConfigViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<AddProductViewModel>();

            Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Try silent auto-login with the saved JWT token.
            // If successful, jump straight to MainWindow.
            // Otherwise, show LoginWindow.
            var auth = Ioc.Default.GetRequiredService<IAuthService>();
            var loggedIn = await auth.TryAutoLoginAsync();

            if (loggedIn)
            {
                _mainWindow = new MainWindow();
                CurrentWindow = _mainWindow;
                _mainWindow.Activate();
                return;
            }

            _loginWindow = new LoginWindow();
            CurrentWindow = _loginWindow;
            _loginWindow.Activate();
        }

        public void OpenMainWindow()
        {
            _mainWindow = new MainWindow();
            CurrentWindow = _mainWindow;
            _mainWindow.Activate();
            _loginWindow?.Close();
            _loginWindow = null;
        }

        public void OpenLoginWindow()
        {
            _loginWindow = new LoginWindow();
            CurrentWindow = _loginWindow;
            _loginWindow.Activate();
            _mainWindow = null;
        }

        /// <summary>
        /// Gets the server URL from local config, falls back to appsettings.json default.
        /// </summary>
        private string GetConfiguredServerUrl()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(ConfigServerUrlKey, out var saved)
                && saved is string url
                && !string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            return Configuration["GraphQL:Endpoint"] ?? "http://localhost:4000/graphql";
        }

        /// <summary>
        /// Saves the server URL to local config (called from ConfigScreen).
        /// </summary>
        public static void SaveServerUrl(string url)
        {
            ApplicationData.Current.LocalSettings.Values[ConfigServerUrlKey] = url;
        }

        /// <summary>
        /// Gets the currently saved server URL.
        /// </summary>
        public static string GetSavedServerUrl()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(ConfigServerUrlKey, out var saved)
                && saved is string url
                && !string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            return Configuration["GraphQL:Endpoint"] ?? "http://localhost:4000/graphql";
        }
    }
}
