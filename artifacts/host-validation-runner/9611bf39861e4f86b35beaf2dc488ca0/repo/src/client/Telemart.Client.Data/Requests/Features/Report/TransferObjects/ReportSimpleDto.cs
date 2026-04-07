namespace Telemart.Client.Data.Requests.Features.Report.TransferObjects
{
    public sealed class ReportSimpleDto
    {
        public int Id { get; init; }

        public string Name { get; init; }

        public string Description { get; init; }

        public bool IsFolder { get; init; }

        public int? ParentId { get; init; }
    }
}