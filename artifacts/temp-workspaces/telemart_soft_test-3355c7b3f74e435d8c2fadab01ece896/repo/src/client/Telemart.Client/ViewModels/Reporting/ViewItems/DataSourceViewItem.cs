using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;

namespace Telemart.Client.ViewModels.Reporting.ViewItems
{
    [POCOViewModel]
    public class DataSourceViewItem
    {
        protected DataSourceViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string Description { get; set; }

        public virtual string Query { get; set; }

        public virtual bool IsSecured { get; set; }

        public virtual DateTime CreatedOn { get; set; }

        public virtual int? CreatedBy { get; set; }

        public virtual DateTime ModifiedOn { get; set; }

        public virtual int? ModifiedBy { get; set; }

        public virtual long Version { get; set; }

        public static DataSourceViewItem Create()
        {
            return ViewModelSource<DataSourceViewItem>.Create();
        }
    }
}