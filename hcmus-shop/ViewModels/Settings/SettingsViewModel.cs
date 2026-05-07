using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace hcmus_shop.ViewModels.Settings
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settings;
        private readonly IBackupService _backup;

        [ObservableProperty]
        private int _pageSize;

        [ObservableProperty]
        private bool _rememberLastScreen;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private bool _isStatusError;

        [ObservableProperty]
        private bool _isBusy;

        public ObservableCollection<int> PageSizeOptions { get; } = new() { 5, 10, 15, 20 };

        /// <summary>Set by SettingsPage so FilePicker can be parented properly.</summary>
        public IntPtr WindowHandle { get; set; }

        public SettingsViewModel(ISettingsService settings, IBackupService backup)
        {
            _settings = settings;
            _backup = backup;
            _pageSize = settings.PageSize;
            _rememberLastScreen = settings.RememberLastScreen;
        }

        [RelayCommand]
        private void Save()
        {
            _settings.PageSize = PageSize;
            _settings.RememberLastScreen = RememberLastScreen;
            SetStatus("Settings saved.", false);
        }

        [RelayCommand]
        private async Task DownloadBackupAsync()
        {
            if (WindowHandle == IntPtr.Zero) { SetStatus("Window handle missing.", true); return; }

            IsBusy = true;
            SetStatus("Preparing backup...", false);
            try
            {
                var picker = new FileSavePicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WindowHandle);
                picker.SuggestedFileName = $"hcmus-shop-backup-{DateTime.Now:yyyyMMdd-HHmm}";
                picker.FileTypeChoices.Add("SQL", new System.Collections.Generic.List<string> { ".sql" });

                var file = await picker.PickSaveFileAsync();
                if (file is null) { SetStatus("Backup cancelled.", false); return; }

                var result = await _backup.DownloadBackupAsync(file.Path);
                if (result.IsSuccess)
                    SetStatus($"Backup saved: {Path.GetFileName(result.Value!)}", false);
                else
                    SetStatus($"Backup failed: {result.Error}", true);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task RestoreAsync()
        {
            if (WindowHandle == IntPtr.Zero) { SetStatus("Window handle missing.", true); return; }

            IsBusy = true;
            SetStatus("Selecting file...", false);
            try
            {
                var picker = new FileOpenPicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WindowHandle);
                picker.FileTypeFilter.Add(".sql");

                var file = await picker.PickSingleFileAsync();
                if (file is null) { SetStatus("Restore cancelled.", false); return; }

                SetStatus("Restoring...", false);
                var result = await _backup.RestoreAsync(file.Path);
                if (result.IsSuccess)
                    SetStatus("Restore complete. Restart the app to see changes.", false);
                else
                    SetStatus($"Restore failed: {result.Error}", true);
            }
            finally { IsBusy = false; }
        }

        private void SetStatus(string message, bool isError)
        {
            StatusMessage = message;
            IsStatusError = isError;
        }
    }
}
