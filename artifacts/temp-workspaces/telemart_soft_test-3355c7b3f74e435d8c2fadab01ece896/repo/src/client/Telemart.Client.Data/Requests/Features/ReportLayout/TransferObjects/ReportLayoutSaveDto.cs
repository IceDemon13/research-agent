namespace Telemart.Client.Data.Requests.Features.ReportLayout.TransferObjects
{
    public class ReportLayoutSaveDto
    {
        public ReportLayoutSaveDto(int id, int reportId, int view, string name, string layout, string parameters)
        {
            Id = id;
            ReportId = reportId;
            Name = name;
            View = view;
            Layout = layout;
            Parameters = parameters;
        }

        public int Id { get; set; }

        public int ReportId { get; set; }

        public string Name { get; set; }

        public int View { get; set; }

        public string Layout { get; set; }

        public string Parameters { get; set; }
    }
}