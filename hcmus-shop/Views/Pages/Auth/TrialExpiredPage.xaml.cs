using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Auth;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace hcmus_shop.Views
{
    public sealed partial class TrialExpiredPage : Page
    {
        public TrialExpiredViewModel ViewModel { get; }

        public TrialExpiredPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<TrialExpiredViewModel>();
            DataContext = ViewModel;

            ViewModel.Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            // After activation, restart the app flow by closing this window
            // and re-running the launch logic (which now passes the trial check).
            if (Application.Current is App app)
            {
                app.RelaunchAfterTrialActivation();
            }
        }
    }
}
