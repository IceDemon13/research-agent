namespace Telemart.Client.Core.Update
{
    public struct WinCheckForUpdateResult
    {
        public string CurrentVersion { get; set; }

        public string FutureVersion { get; set; }

        public ReleaseToApply[] ReleasesToApply { get; set; }
    }
}