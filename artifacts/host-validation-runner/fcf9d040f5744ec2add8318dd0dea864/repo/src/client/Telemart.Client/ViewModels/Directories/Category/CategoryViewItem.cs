using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;

namespace Telemart.Client.ViewModels.Directories.Category
{
    [POCOViewModel]
    public class CategoryViewItem
    {
        protected CategoryViewItem()
        {
        }

        public virtual double Active { get; set; }

        public virtual int Id { get; set; }

        public virtual bool IsParent { get; set; }

        public virtual int Left { get; set; }

        public virtual int Right { get; set; }

        public virtual int Level { get; set; }

        public virtual int ParentLevel { get; set; }

        public virtual string Name { get; set; }

        public virtual string NameFull { get; set; }

        public virtual string NameFullUkr { get; set; }

        public virtual int ParentId { get; set; }

        public virtual int Position { get; set; }

        public virtual string Version { get; set; }

        public int YandexMarketHid { get; set; }

        public virtual bool? Selected { get; set; }

        public virtual bool Enabled { get; set; }

        public virtual string Manufactor { get; set; }

        public virtual int EmployeeId { get; set; }

        public virtual bool UseInTradeIn { get; set; }

        public string NameWithId => $"{Name} ({Id})";

        public bool IsBrand => !IsParent && string.Equals(Name, Manufactor, StringComparison.Ordinal);

        public bool IsFolder => !IsParent && !string.Equals(Name, Manufactor, StringComparison.Ordinal);

        public static CategoryViewItem Create()
        {
            return ViewModelSource<CategoryViewItem>.Create();
        }

        protected void OnIdChanged(int oldValue)
        {
            this.RaisePropertyChanged(x => x.NameWithId);
        }

        protected void OnIsParentChanged(bool oldValue)
        {
            this.RaisePropertyChanged(x => x.NameWithId);
        }

        protected void OnNameChanged(string oldValue)
        {
            this.RaisePropertyChanged(x => x.IsBrand);
            this.RaisePropertyChanged(x => x.IsFolder);
        }
    }
}