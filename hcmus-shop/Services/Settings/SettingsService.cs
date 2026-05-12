using hcmus_shop.Contracts.Services;
using Windows.Storage;

namespace hcmus_shop.Services.Settings
{
    public class SettingsService : ISettingsService
    {
        private const string PageSizeKey = "settings_page_size";
        private const string RememberLastScreenKey = "settings_remember_last_screen";
        private const string LastScreenKey = "settings_last_screen";

        private const int DefaultPageSize = 10;

        private static Windows.Foundation.Collections.IPropertySet Store
            => ApplicationData.Current.LocalSettings.Values;

        public event System.EventHandler? SettingsChanged;

        public int PageSize
        {
            get => Store[PageSizeKey] is int v && v > 0 ? v : DefaultPageSize;
            set
            {
                var normalizedValue = value > 0 ? value : DefaultPageSize;
                if (PageSize == normalizedValue)
                {
                    return;
                }

                Store[PageSizeKey] = normalizedValue;
                SettingsChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

        public bool RememberLastScreen
        {
            get => Store[RememberLastScreenKey] is bool b && b;
            set
            {
                if (RememberLastScreen == value)
                {
                    return;
                }

                Store[RememberLastScreenKey] = value;
                SettingsChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

        public string? LastScreen
        {
            get => Store[LastScreenKey] as string;
            set
            {
                var currentValue = LastScreen;
                if (string.Equals(currentValue, value, System.StringComparison.Ordinal))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(value)) Store.Remove(LastScreenKey);
                else Store[LastScreenKey] = value;

                SettingsChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }
    }
}
