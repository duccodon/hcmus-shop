using hcmus_shop.Contracts.Services;
using System;
using Windows.Storage;

namespace hcmus_shop.Services.Trial
{
    public class TrialService : ITrialService
    {
        private const string StartDateKey = "trial_start_date";
        private const string ActivatedKey = "trial_activated";
        private const int TrialDays = 15;

        // Demo activation code. In production this would be validated against a server
        // or a signed license file.
        private const string ValidCode = "HCMUS2026";

        private static Windows.Foundation.Collections.IPropertySet Store
            => ApplicationData.Current.LocalSettings.Values;

        public TrialStatus GetStatus()
        {
            if (Store[ActivatedKey] is bool activated && activated)
                return TrialStatus.Activated;

            DateTime start;
            if (Store[StartDateKey] is string saved && DateTime.TryParse(saved, out var parsed))
            {
                start = parsed;
            }
            else
            {
                start = DateTime.UtcNow;
                Store[StartDateKey] = start.ToString("o"); // ISO 8601 round-trip
            }

            var elapsed = (DateTime.UtcNow - start).TotalDays;
            return elapsed > TrialDays ? TrialStatus.Expired : TrialStatus.Active;
        }

        public int DaysRemaining
        {
            get
            {
                if (Store[StartDateKey] is string saved && DateTime.TryParse(saved, out var start))
                {
                    var remaining = TrialDays - (int)(DateTime.UtcNow - start).TotalDays;
                    return remaining < 0 ? 0 : remaining;
                }
                return TrialDays;
            }
        }

        public bool Activate(string code)
        {
            if (!string.Equals(code?.Trim(), ValidCode, StringComparison.Ordinal))
                return false;

            Store[ActivatedKey] = true;
            return true;
        }
    }
}
