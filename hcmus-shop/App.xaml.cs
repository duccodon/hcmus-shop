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
using hcmus_shop.Services.Products.Dto;
using hcmus_shop.Services.Config;
using hcmus_shop.Services.Dashboard;
using hcmus_shop.Services.Settings;
using hcmus_shop.Services.License;
using hcmus_shop.Services.Onboarding;
using hcmus_shop.Services.Backup;
using hcmus_shop.Services.Health;
using hcmus_shop.Services.Promotions;
using hcmus_shop.Services.Customers;
using hcmus_shop.Services.Orders;
using hcmus_shop.Services.Reports;
using hcmus_shop.Services.Invoices;
using hcmus_shop.Services.Uploads;
using hcmus_shop.Services.Users;
using hcmus_shop.ViewModels;
using hcmus_shop.ViewModels.Admin;
using hcmus_shop.ViewModels.Auth;
using hcmus_shop.ViewModels.Customers;
using hcmus_shop.ViewModels.Orders;
using hcmus_shop.ViewModels.Products;
using hcmus_shop.ViewModels.Reports;
using hcmus_shop.ViewModels.Settings;
using hcmus_shop.ViewModels.Promotions;
using hcmus_shop.ViewModels.Store;
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

            services.AddSingleton<IBrandService, BrandService>();
            services.AddSingleton<ICategoryService, CategoryService>();
            services.AddSingleton<ISeriesService, SeriesService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<IProductImportService, ProductImportService>();
            services.AddSingleton<IDashboardService, DashboardService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ILicenseService, LicenseService>();
            services.AddSingleton<IOnboardingService, OnboardingService>();
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<IHealthService, HealthService>();
            services.AddSingleton<IPromotionService, PromotionService>();
            services.AddSingleton<ICustomerService, CustomerService>();
            services.AddSingleton<IOrderService, OrderService>();
            services.AddSingleton<IReportService, ReportService>();
            services.AddSingleton<IInvoiceService, InvoiceService>();
            services.AddSingleton<IFileUploadService, FileUploadService>();
            services.AddSingleton<IUserService, UserService>();

            // ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<ConfigViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<TrialExpiredViewModel>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<AddProductViewModel>();
            services.AddTransient<EditProductViewModel>();
            services.AddTransient<PromotionsViewModel>();
            services.AddTransient<CustomersViewModel>();
            services.AddTransient<CustomerDetailViewModel>();
            services.AddTransient<OrdersViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<StoreViewModel>();
            services.AddTransient<StoreDetailViewModel>();
            services.AddTransient<SalesUsersViewModel>();

            Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {           
            // 1. License/trial check — if expired (trial OR license), block everything.
            var license = Ioc.Default.GetRequiredService<ILicenseService>();
            if (license.GetStatus() == LicenseStatus.Expired)
            {
                ShowTrialExpiredWindow();
                return;
            }

            // 2. Try silent auto-login with the saved JWT token.
            var auth = Ioc.Default.GetRequiredService<IAuthService>();
            var loggedIn = await auth.TryAutoLoginAsync();

            if (loggedIn)
            {
                _mainWindow = new MainWindow();
                CurrentWindow = _mainWindow;
                _mainWindow.Activate();
                return;
            }

            // 3. Otherwise show LoginWindow.
            _loginWindow = new LoginWindow();
            CurrentWindow = _loginWindow;
            _loginWindow.Activate();
        }

        private Window? _trialWindow;

        private void ShowTrialExpiredWindow()
        {
            _trialWindow = new TrialExpiredWindow();
            CurrentWindow = _trialWindow;
            _trialWindow.Activate();
        }

        /// <summary>
        /// Called from TrialExpiredPage after the user enters a valid code.
        /// Important: open the next window FIRST, then close the trial window.
        /// If we close the trial window before awaiting, the await continuation
        /// runs on a destroyed dispatcher context and crashes.
        /// </summary>
        public async void RelaunchAfterTrialActivation()
        {
            var auth = Ioc.Default.GetRequiredService<IAuthService>();
            var loggedIn = await auth.TryAutoLoginAsync();

            if (loggedIn)
            {
                _mainWindow = new MainWindow();
                CurrentWindow = _mainWindow;
                _mainWindow.Activate();
            }
            else
            {
                _loginWindow = new LoginWindow();
                CurrentWindow = _loginWindow;
                _loginWindow.Activate();
            }

            // Now safe to close the trial window — there's another active window.
            var trial = _trialWindow;
            _trialWindow = null;
            trial?.Close();
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
