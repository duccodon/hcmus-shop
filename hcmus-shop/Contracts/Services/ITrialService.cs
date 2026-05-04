namespace hcmus_shop.Contracts.Services
{
    public enum TrialStatus
    {
        Active,
        Expired,
        Activated
    }

    /// <summary>
    /// Trial mode: 15-day lock with activation code.
    /// On first launch, the trial start date is recorded. After 15 days the
    /// app blocks access until the user enters the activation code.
    /// </summary>
    public interface ITrialService
    {
        TrialStatus GetStatus();
        bool Activate(string code);
        int DaysRemaining { get; }
    }
}
