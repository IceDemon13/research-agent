using System;

namespace Telemart.Client.Data.Requests.Features.Report.TransferObjects
{
    public class ReportDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        public int? ParentId { get; set; }

        public int DatabaseId { get; init; }

        public long Version { get; set; }

        public string[] Roles { get; set; }

        public int[] Employees { get; set; }

        public int? TimeoutSeconds { get; set; }

        public ReportParameterDto[] Parameters { get; set; }

        public string Query { get; set; }

        public ReportFieldDto[] Fields { get; set; }

        public bool IsFolder { get; init; }
    }
}