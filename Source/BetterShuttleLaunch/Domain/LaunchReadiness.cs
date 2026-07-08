namespace BetterShuttleLaunch.Domain
{
    public readonly struct LaunchReadiness
    {
        public LaunchReadiness(bool canLaunchNow, bool shouldCancel, string statusText, string failureReason)
        {
            CanLaunchNow = canLaunchNow;
            ShouldCancel = shouldCancel;
            StatusText = statusText;
            FailureReason = failureReason;
        }

        public bool CanLaunchNow { get; }
        public bool ShouldCancel { get; }
        public string StatusText { get; }
        public string FailureReason { get; }
    }
}
