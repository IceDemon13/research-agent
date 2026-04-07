namespace Telemart.Client.Data.Requests.Features.Report.TransferObjects
{
    public class ReportParameterDto
    {
        public string Name { get; set; }

        public string SqlName { get; set; }

        public int EditorType { get; set; }

        public string DataSource { get; set; }

        public bool DataSourceIsSecured { get; set; }

        public int? DataSourceId { get; set; }
    }
}