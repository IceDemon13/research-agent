namespace Telemart.Client.Data.Requests.Features.Report.TransferObjects
{
    public class ReportSaveDto
    {
        public int Id { get; set; }

        public int? ParentId { get; init; }

        public string Name { get; init; }

        public int DatabaseId { get; init; }

        public string Description { get; init; }

        //// Json data properties

        public string[] Roles { get; init; }

        public int[] Employees { get; init; }

        public int? TimeoutSeconds { get; init; }

        public ReportParameterDto[] Parameters { get; init; }

        public string Query { get; init; }

        public ReportFieldDto[] Fields { get; init; }
    }
}