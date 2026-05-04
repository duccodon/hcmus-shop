using hcmus_shop.Contracts.Services;
using Windows.Storage;

namespace hcmus_shop.Services.Onboarding
{
    public class OnboardingService : IOnboardingService
    {
        private const string CompletedKey = "onboarding_completed";

        private static Windows.Foundation.Collections.IPropertySet Store
            => ApplicationData.Current.LocalSettings.Values;

        public bool IsCompleted => Store[CompletedKey] is bool b && b;

        public void MarkCompleted() => Store[CompletedKey] = true;

        public void Reset() => Store.Remove(CompletedKey);
    }
}
