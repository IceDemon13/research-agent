using System;

namespace Telemart.Client.ViewModels.Reporting
{
    internal sealed class ReportLayoutsParameter
    {
        public ReportLayoutsParameter(
            int reportId,
            Guid reportEditorId,
            ReportLayoutType layoutType,
            bool isLoad,
            string currentLayout,
            ReportLayoutParameter[] currentParameters)
        {
            ReportId = reportId;
            ReportEditorId = reportEditorId;
            LayoutType = layoutType;
            IsLoad = isLoad;
            CurrentLayout = currentLayout;
            CurrentParameters = currentParameters;
        }

        public int ReportId { get; }

        public Guid ReportEditorId { get; }

        public string CurrentLayout { get; }

        public ReportLayoutParameter[] CurrentParameters { get; }

        public ReportLayoutType LayoutType { get; }

        public bool IsLoad { get; }
    }
}