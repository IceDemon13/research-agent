using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Bitrix
{
    public class CategorizedBitrixTaskViewItem : BindableBase, IDataErrorInfo
    {
        public CategorizedBitrixTaskViewItem()
        {
            CategoryIds = new ObservableCollection<int>();
            Processed = false;
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int BitrixId
        {
            get { return GetProperty(() => BitrixId); }
            set { SetProperty(() => BitrixId, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public Priority Priority
        {
            get { return GetProperty(() => Priority); }
            set { SetProperty(() => Priority, value); }
        }

        public Priority OurPriority
        {
            get { return GetProperty(() => OurPriority); }
            set { SetProperty(() => OurPriority, value); }
        }

        public int? RoleId
        {
            get { return GetProperty(() => RoleId); }
            set { SetProperty(() => RoleId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string CreatedByName
        {
            get { return GetProperty(() => CreatedByName); }
            set { SetProperty(() => CreatedByName, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public DateTime? Deadline
        {
            get { return GetProperty(() => Deadline); }
            set { SetProperty(() => Deadline, value); }
        }

        public ObservableCollection<int> CategoryIds
        {
            get { return GetProperty(() => CategoryIds); }
            set { SetProperty(() => CategoryIds, value, OnCategoryIdsChanged); }
        }

        public string Error
        {
            get { return GetProperty(() => Error); }
            set { SetProperty(() => Error, value, () => RaisePropertyChanged(nameof(IsSuccess))); }
        }

        public bool Processed
        {
            get { return GetProperty(() => Processed); }
            set { SetProperty(() => Processed, value, () => RaisePropertyChanged(nameof(IsSuccess))); }
        }

        public bool IsSuccess => Processed && string.IsNullOrWhiteSpace(Error);

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<CategorizedBitrixTaskViewItem> builder)
        {
            builder.Property(x => x.OurPriority).Required(() => Resources.RequiredErrorMessage);
        }

        public override string ToString() => Name;

        private void OnCategoryIdsChanged(ObservableCollection<int> old)
        {
            Error = CategoryIds?.Any() == true
                ? null
                : "Не выбрана ни одна категория";
        }
    }
}
