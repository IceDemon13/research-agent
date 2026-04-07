namespace Telemart.Client.Core.IO
{
    public sealed class DownloadFileProgressInfo
    {
        public DownloadFileProgressInfo(long bytesReceived, long totalBytesToReceive, int progressPercentage)
        {
            BytesReceived = bytesReceived;
            TotalBytesToReceive = totalBytesToReceive;
            ProgressPercentage = progressPercentage;
        }

        public long BytesReceived { get; }

        public int ProgressPercentage { get; }

        public long TotalBytesToReceive { get; }
    }
}