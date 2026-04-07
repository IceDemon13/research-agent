using System;

namespace Telemart.Client.Data.Requests.Features.ReportLayout.TransferObjects
{
    public class ReportLayoutDto
    {
        public int Id { get; set; }

        public int ReportId { get; set; }

        public string Name { get; set; }

        public int View { get; set; }

        public string Layout { get; set; }

        public string Parameters { get; set; }

        public DateTime CreatedOn { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        public int? ModifiedBy { get; set; }

        public long Version { get; set; }
    }
}