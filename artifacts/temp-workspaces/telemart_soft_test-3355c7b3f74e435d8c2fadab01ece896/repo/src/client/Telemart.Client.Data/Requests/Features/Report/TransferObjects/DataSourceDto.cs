using System;

namespace Telemart.Client.Data.Requests.Features.Report.TransferObjects
{
    public class DataSourceDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Query { get; set; }

        public bool IsSecured { get; set; }

        public DateTime CreatedOn { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        public int? ModifiedBy { get; set; }

        public long Version { get; set; }
    }
}