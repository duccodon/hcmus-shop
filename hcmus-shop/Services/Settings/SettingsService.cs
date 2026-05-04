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

        public int PageSize
        {
            get => Store[PageSizeKey] is int v && v > 0 ? v : DefaultPageSize;
            set => Store[PageSizeKey] = value > 0 ? value : DefaultPageSize;
        }

        public bool RememberLastScreen
        {
            get => Store[RememberLastScreenKey] is bool b && b;
            set => Store[RememberLastScreenKey] = value;
        }

        public string? LastScreen
        {
            get => Store[LastScreenKey] as string;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) Store.Remove(LastScreenKey);
                else Store[LastScreenKey] = value;
            }
        }
    }
}
