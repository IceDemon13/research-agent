using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;

namespace Telemart.Client.ViewModels.Reporting.ViewItems
{
    [POCOViewModel]
    public class ReportLayoutViewItem
    {
        public virtual int Id { get; set; }

        public virtual int ReportId { get; set; }

        public virtual string Name { get; set; }

        public virtual ReportLayoutType View { get; set; }

        public virtual string Layout { get; set; }

        public virtual string Parameters { get; set; }

        public virtual DateTime CreatedOn { get; set; }

        public virtual int? CreatedBy { get; set; }

        public virtual DateTime ModifiedOn { get; set; }

        public virtual int? ModifiedBy { get; set; }

        public virtual long Version { get; set; }

        public static ReportLayoutViewItem Create()
        {
            return ViewModelSource<ReportLayoutViewItem>.Create();
        }
    }
}