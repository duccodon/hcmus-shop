using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage()
        {
            ViewModel = Ioc.Default.GetRequiredService<SettingsViewModel>();
            InitializeComponent();
            DataContext = ViewModel;

            // FilePickers in WinUI 3 desktop need an HWND. Wire it up after page is loaded.
            Loaded += (s, e) =>
            {
                if (Application.Current is App app && app.CurrentWindow is Window window)
                {
                    ViewModel.WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                }
            };
        }
    }
}
