namespace Telemart.Client.Common.Settings.ModuleAnalytics
{
    public record ModuleAnalyticsSetting
    {
        public int PositionId { get; init; }

        public int LocationId { get; init; }

        public int? Width { get; init; }

        public int? Height { get; init; }

        public int? Top { get; init; }

        public int? Left { get; init; }
    }
}