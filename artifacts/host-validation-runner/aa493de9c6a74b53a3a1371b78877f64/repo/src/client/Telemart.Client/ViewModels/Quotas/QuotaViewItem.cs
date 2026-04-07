using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Quotas
{
    public sealed class QuotaViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public DateTime QuotaDate
        {
            get { return GetProperty(() => QuotaDate); }
            set { SetProperty(() => QuotaDate, value); }
        }

        public int DepartmentId
        {
            get { return GetProperty(() => DepartmentId); }
            set { SetProperty(() => DepartmentId, value); }
        }

        public int Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }
    }
}