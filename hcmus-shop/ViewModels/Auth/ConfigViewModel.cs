using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Auth
{
    public partial class ConfigViewModel : ObservableObject
    {
        private readonly IConfigService _config;
        private readonly IGraphQLClientService _graphQL;

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

        public ConfigViewModel(IConfigService config, IGraphQLClientService graphQL)
        {
            _config = config;
            _graphQL = graphQL;
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

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var content = new StringContent(
                    "{\"query\":\"{ __typename }\"}",
                    System.Text.Encoding.UTF8,
                    "application/json");
                var response = await http.PostAsync(ServerUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    SetStatus("Connection successful.", false);
                }
                else
                {
                    SetStatus($"Server responded with {(int)response.StatusCode}.", true);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Connection failed: {ex.Message}", true);
            }
            finally
            {
                IsTesting = false;
            }
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
