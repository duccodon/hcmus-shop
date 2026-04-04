using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views
{
    public sealed partial class MessagesPage : Page
    {
        public MessagesPage()
        {
            InitializeComponent();
        }

        // ── Event handlers for the test CheckBox ─────────────────────────────────────
        private void OnChecked(object sender, RoutedEventArgs e)
        {
            // TODO: Add your logic here (e.g. enable a button, log, etc.)
            // Example:
            // System.Diagnostics.Debug.WriteLine("✅ Checkbox checked");
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Add your logic here
            // Example:
            // System.Diagnostics.Debug.WriteLine("❌ Checkbox unchecked");
        }
    }
}
