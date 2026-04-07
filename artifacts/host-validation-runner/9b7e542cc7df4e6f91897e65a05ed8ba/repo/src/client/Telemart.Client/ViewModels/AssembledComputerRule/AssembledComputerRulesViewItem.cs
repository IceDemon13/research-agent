using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssembledComputerRulesViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string BaseProductName
        {
            get { return GetProperty(() => BaseProductName); }
            set { SetProperty(() => BaseProductName, value); }
        }

        public string PartNumber
        {
            get { return GetProperty(() => PartNumber); }
            set { SetProperty(() => PartNumber, value); }
        }

        public string GroupName
        {
            get { return GetProperty(() => GroupName); }
            set { SetProperty(() => GroupName, value); }
        }

        public string Prefix
        {
            get { return GetProperty(() => Prefix); }
            set { SetProperty(() => Prefix, value); }
        }

        public string Link
        {
            get { return GetProperty(() => Link); }
            set { SetProperty(() => Link, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public string Color
        {
            get { return GetProperty(() => Color); }
            set { SetProperty(() => Color, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }
    }
}