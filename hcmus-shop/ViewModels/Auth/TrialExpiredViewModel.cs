using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using System;

namespace hcmus_shop.ViewModels.Auth
{
    public partial class TrialExpiredViewModel : ObservableObject
    {
        private readonly ILicenseService _license;

        [ObservableProperty]
        private string _activationCode = string.Empty;

        [ObservableProperty]
        private string? _errorMessage;

        public event EventHandler? Activated;

        public TrialExpiredViewModel(ILicenseService license)
        {
            _license = license;
        }

        [RelayCommand]
        private void Activate()
        {
            ErrorMessage = null;
            if (string.IsNullOrWhiteSpace(ActivationCode))
            {
                ErrorMessage = "Please enter the activation code.";
                return;
            }

            if (!_license.Activate(ActivationCode))
            {
                ErrorMessage = "Invalid activation code.";
                return;
            }

            Activated?.Invoke(this, EventArgs.Empty);
        }
    }
}
