namespace BetterShuttleLaunch.Domain
{
    public readonly struct ShuttleCommandState
    {
        public ShuttleCommandState(ShuttleContext context, bool hasQueuedLaunch)
        {
            Context = context;
            HasQueuedLaunch = hasQueuedLaunch;
        }

        public ShuttleContext Context { get; }
        public bool HasQueuedLaunch { get; }
    }
}
