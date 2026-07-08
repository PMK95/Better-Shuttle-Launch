namespace BetterShuttleLaunch.Domain
{
    public enum LaunchIntent
    {
        ScheduleWhenReady,
        ReturnWithLandingSelection,
        ReturnToLastDepartureCell,
        LaunchToSettlement,
        CancelQueuedLaunch
    }
}
