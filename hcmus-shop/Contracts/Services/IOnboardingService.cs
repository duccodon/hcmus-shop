namespace hcmus_shop.Contracts.Services
{
    public interface IOnboardingService
    {
        bool IsCompleted { get; }
        void MarkCompleted();
        void Reset();
    }
}
