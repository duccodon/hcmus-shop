using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using System;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Auth
{
    public partial class ConfigViewModel : ObservableObject
    {
        private readonly IConfigService _config;
        private readonly IGraphQLClientService _graphQL;
        private readonly IHealthService _healthService;

        [ObservableProperty]
        private string _serverUrl = string.Empty;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private bool _isStatusError;

        [ObservableProperty]
        private bool _isTesting;

        public event EventHandler? Saved;
        public event EventHandler? Cancelled;

        public ConfigViewModel(
            IConfigService config,
            IGraphQLClientService graphQL,
            IHealthService healthService)
        {
            _config = config;
            _graphQL = graphQL;
            _healthService = healthService;
            ServerUrl = _config.GetServerUrl();
        }

        /// <summary>
        /// Reloads the current saved server URL into the form. Called by ConfigPage
        /// on Loaded to guarantee the field shows the latest saved URL even if the
        /// VM was constructed before the saved value changed.
        /// </summary>
        public void RefreshFromConfig()
        {
            ServerUrl = _config.GetServerUrl();
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(ServerUrl))
            {
                SetStatus("Server URL is required.", true);
                return;
            }

            IsTesting = true;
            SetStatus("Testing...", false);

            // Ping the /health endpoint of the URL the user typed (not the saved one).
            var result = await _healthService.PingAsync(ServerUrl.Trim());

            IsTesting = false;
            if (result.IsSuccess)
                SetStatus("Connection successful.", false);
            else
                SetStatus($"Connection failed: {result.Error}", true);
        }

        [RelayCommand]
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(ServerUrl))
            {
                SetStatus("Server URL is required.", true);
                return;
            }

            _config.SetServerUrl(ServerUrl.Trim());
            _graphQL.SetServerUrl(ServerUrl.Trim());
            Saved?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void ResetToDefault()
        {
            ServerUrl = _config.GetDefaultServerUrl();
            SetStatus("Reset to default URL.", false);
        }

        private void SetStatus(string message, bool isError)
        {
            StatusMessage = message;
            IsStatusError = isError;
        }
    }
}
