using System;
using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderBillViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Bill1cId
        {
            get { return GetProperty(() => Bill1cId); }
            set { SetProperty(() => Bill1cId, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value); }
        }

        public int OrganizationAccountId
        {
            get { return GetProperty(() => OrganizationAccountId); }
            set { SetProperty(() => OrganizationAccountId, value); }
        }

        public int OrganizationId
        {
            get { return GetProperty(() => OrganizationId); }
            set { SetProperty(() => OrganizationId, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public DateTime ExpireDate
        {
            get { return GetProperty(() => ExpireDate); }
            set { SetProperty(() => ExpireDate, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public Payment Payment
        {
            get { return GetProperty(() => Payment); }
            set { SetProperty(() => Payment, value); }
        }
    }
}
