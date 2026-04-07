using System;
using Telemart.Client.ViewModels.Reporting;

namespace Telemart.Client.Common.Messages
{
    public sealed class LoadReportLayoutMessage
    {
        public LoadReportLayoutMessage(int reportId, Guid reportEditorId, ReportLayoutType view, string layout, ReportLayoutParameter[] parameters)
        {
            ReportId = reportId;
            ReportEditorId = reportEditorId;
            View = view;
            Layout = layout;
            Parameters = parameters;
        }

        public int ReportId { get; }

        public Guid ReportEditorId { get; }

        public string Layout { get; }

        public ReportLayoutParameter[] Parameters { get; }

        public ReportLayoutType View { get; }
    }
}