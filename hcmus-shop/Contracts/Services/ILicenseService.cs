namespace hcmus_shop.Contracts.Services
{
    public enum LicenseStatus
    {
        /// <summary>App is usable — either inside the trial window or with a valid license.</summary>
        Active,

        /// <summary>App is locked — must enter an activation code to continue.</summary>
        Expired,
    }

    /// <summary>
    /// License & trial gate.
    ///
    /// First launch: trial period of 15 days starts (no activation needed).
    /// During trial: app works normally; UI may show "Trial: X days left".
    /// Trial expires after 15 days: user must enter activation code.
    /// Activation: stores activation date; license valid for 365 days.
    /// License expires after 365 days: user must re-activate.
    ///
    /// Both trial-expired and license-expired states surface as the same
    /// LicenseStatus.Expired and route to the same activation page.
    /// </summary>
    public interface ILicenseService
    {
        LicenseStatus GetStatus();

        /// <summary>True when the user is in the free trial (no valid activation yet).</summary>
        bool IsTrial { get; }

        /// <summary>Days remaining in the current period (trial: ≤15, licensed: ≤365).</summary>
        int DaysRemaining { get; }

        /// <summary>
        /// Validates the activation code and persists the activation date.
        /// Returns true on success, false on invalid code.
        /// </summary>
        bool Activate(string code);
    }
}
