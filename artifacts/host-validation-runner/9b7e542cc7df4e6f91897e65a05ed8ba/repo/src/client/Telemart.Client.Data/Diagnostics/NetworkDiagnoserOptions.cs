namespace Telemart.Client.Data.Diagnostics
{
    public class NetworkDiagnoserOptions
    {
        public string PingAddress { get; init; }

        public int PingRetriesCount { get; init; }

        public int RequestAnalizePeriod { get; init; }

        public bool Disabled { get; init; }
    }
}