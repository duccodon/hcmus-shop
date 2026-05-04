using Microsoft.UI.Xaml;
using hcmus_shop.Views;

namespace hcmus_shop
{
    /// <summary>
    /// Shown instead of LoginWindow when the trial has expired.
    /// Closes after successful activation; App.RelaunchAfterTrialActivation
    /// then opens the next correct window (MainWindow or LoginWindow).
    /// </summary>
    public sealed class TrialExpiredWindow : Window
    {
        public TrialExpiredWindow()
        {
            Title = "HCMUS Shop";
            Content = new TrialExpiredPage();
        }
    }
}
