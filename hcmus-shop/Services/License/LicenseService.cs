using hcmus_shop.Contracts.Services;
using System;
using Windows.Storage;

namespace hcmus_shop.Services.License
{
    public class LicenseService : ILicenseService
    {
        // Storage keys (named to reflect intent — `trial_activated` was misleading
        // because it was used for both trial and license-expired paths).
        private const string TrialStartKey = "trial_start_date";
        private const string LicenseActivatedKey = "license_activated_date";

        private const int TrialDays = 15;
        private const int LicenseDays = 365;

        // Demo activation code. Production would validate against a server or
        // a signed license file bound to the machine.
        private const string ValidCode = "HCMUS2026";

        private static Windows.Foundation.Collections.IPropertySet Store
            => ApplicationData.Current.LocalSettings.Values;

        public LicenseStatus GetStatus()
        {
            // Priority 1: a valid (non-expired) license overrides trial state.
            var licenseDate = ParseDate(LicenseActivatedKey);
            if (licenseDate.HasValue)
            {
                var elapsed = (DateTime.UtcNow - licenseDate.Value).TotalDays;
                return elapsed > LicenseDays ? LicenseStatus.Expired : LicenseStatus.Active;
            }

            // Priority 2: in trial period?
            var trialStart = ParseDate(TrialStartKey);
            if (!trialStart.HasValue)
            {
                // First launch — record now, return Active
                Store[TrialStartKey] = DateTime.UtcNow.ToString("o");
                return LicenseStatus.Active;
            }

            var trialElapsed = (DateTime.UtcNow - trialStart.Value).TotalDays;
            return trialElapsed > TrialDays ? LicenseStatus.Expired : LicenseStatus.Active;
        }

        public bool IsTrial
        {
            get
            {
                var licenseDate = ParseDate(LicenseActivatedKey);
                if (licenseDate.HasValue)
                {
                    var elapsed = (DateTime.UtcNow - licenseDate.Value).TotalDays;
                    if (elapsed <= LicenseDays) return false; // licensed
                }
                return true; // trial (active or expired) or expired license
            }
        }

        public int DaysRemaining
        {
            get
            {
                var licenseDate = ParseDate(LicenseActivatedKey);
                if (licenseDate.HasValue)
                {
                    var elapsed = (DateTime.UtcNow - licenseDate.Value).TotalDays;
                    var remaining = LicenseDays - (int)elapsed;
                    return remaining < 0 ? 0 : remaining;
                }

                var trialStart = ParseDate(TrialStartKey);
                if (trialStart.HasValue)
                {
                    var elapsed = (DateTime.UtcNow - trialStart.Value).TotalDays;
                    var remaining = TrialDays - (int)elapsed;
                    return remaining < 0 ? 0 : remaining;
                }

                return TrialDays;
            }
        }

        public bool Activate(string code)
        {
            if (!string.Equals(code?.Trim(), ValidCode, StringComparison.Ordinal))
                return false;

            // Store the activation DATE — license is valid for 365 days from this point.
            Store[LicenseActivatedKey] = DateTime.UtcNow.ToString("o");
            return true;
        }

        private static DateTime? ParseDate(string key)
        {
            if (Store[key] is string raw && DateTime.TryParse(raw, out var date))
                return date;
            return null;
        }
    }
}
