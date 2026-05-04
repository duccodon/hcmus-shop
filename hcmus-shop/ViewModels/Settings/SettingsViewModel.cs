using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using System.Collections.ObjectModel;

namespace hcmus_shop.ViewModels.Settings
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settings;

        [ObservableProperty]
        private int _pageSize;

        [ObservableProperty]
        private bool _rememberLastScreen;

        [ObservableProperty]
        private string? _statusMessage;

        public ObservableCollection<int> PageSizeOptions { get; } = new() { 5, 10, 15, 20 };

        public SettingsViewModel(ISettingsService settings)
        {
            _settings = settings;
            _pageSize = settings.PageSize;
            _rememberLastScreen = settings.RememberLastScreen;
        }

        [RelayCommand]
        private void Save()
        {
            _settings.PageSize = PageSize;
            _settings.RememberLastScreen = RememberLastScreen;
            StatusMessage = "Settings saved.";
        }
    }
}
