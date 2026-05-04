using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Auth;
using Microsoft.UI.Xaml.Controls;
using System;

namespace hcmus_shop.Views
{
    public sealed partial class ConfigPage : Page
    {
        public ConfigViewModel ViewModel { get; }

        public event EventHandler? Saved;
        public event EventHandler? Cancelled;

        public ConfigPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<ConfigViewModel>();
            DataContext = ViewModel;

            ViewModel.Saved += (s, e) => Saved?.Invoke(this, EventArgs.Empty);
            ViewModel.Cancelled += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}
