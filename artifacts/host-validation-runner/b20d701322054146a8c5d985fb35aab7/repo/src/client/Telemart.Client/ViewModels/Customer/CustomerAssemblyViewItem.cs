using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Customer
{
    public class CustomerAssemblyViewItem : TelemartViewItemBase
    {
        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public string ProductLink
        {
            get { return GetProperty(() => ProductLink); }
            set { SetProperty(() => ProductLink, value); }
        }

        public int AssemblyId
        {
            get { return GetProperty(() => AssemblyId); }
            set { SetProperty(() => AssemblyId, value); }
        }

        public bool CustomerDeleted
        {
            get { return GetProperty(() => CustomerDeleted); }
            set { SetProperty(() => CustomerDeleted, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }
    }
}