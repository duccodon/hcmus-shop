using hcmus_shop.Contracts.Services;
using Windows.Storage;

namespace hcmus_shop.Services.Onboarding
{
    /// <summary>
    /// Tracks onboarding-tour completion PER USER (per machine).
    /// The flag key is namespaced by the current logged-in username, so
    /// different users on the same Windows account each see the tour once.
    /// </summary>
    public class OnboardingService : IOnboardingService
    {
        private readonly IAuthService _authService;

        public OnboardingService(IAuthService authService)
        {
            _authService = authService;
        }

        private static Windows.Foundation.Collections.IPropertySet Store
            => ApplicationData.Current.LocalSettings.Values;

        private string Key
        {
            get
            {
                // If somehow called before login, fall back to a generic key so we
                // don't crash; the post-login check will use the proper user key.
                var username = _authService.CurrentUser?.Username ?? "_anonymous";
                return $"onboarding_completed::{username}";
            }
        }

        public bool IsCompleted => Store[Key] is bool b && b;

        public void MarkCompleted() => Store[Key] = true;

        public void Reset() => Store.Remove(Key);
    }
}
