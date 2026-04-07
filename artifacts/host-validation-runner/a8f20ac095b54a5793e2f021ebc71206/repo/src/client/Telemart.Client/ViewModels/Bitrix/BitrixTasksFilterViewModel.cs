using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Bitrix
{
    public class BitrixTasksFilterViewModel : BindableBase, IDataErrorInfo
    {
        public BitrixTasksFilterViewModel(IDictionaries dictionaries)
        {
            Roles = dictionaries.GetItems<BitrixTaskRole>().ToReadOnlyObservableCollection();
        }

        public BitrixTasksFilterViewModel()
        {
        }

        #region Collections

        public ReadOnlyObservableCollection<BitrixTaskRole> Roles { get; }

        #endregion

        public string BitrixIds
        {
            get { return GetProperty(() => BitrixIds); }
            set { SetProperty(() => BitrixIds, value); }
        }

        public ObservableCollection<int> SelectedRoles
        {
            get { return GetProperty(() => SelectedRoles); }
            set { SetProperty(() => SelectedRoles, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<BitrixTasksFilterViewModel> builder)
        {
            builder.Property(x => x.BitrixIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public BitrixTasksFilteringItem GetFilteringItem()
        {
            BitrixTasksFilteringItem item = new BitrixTasksFilteringItem(
                BitrixIds,
                SelectedRoles?.ToArray(),
                Array.Empty<int>());

            return item;
        }

        public void ResetFilterValues()
        {
            BitrixIds = null;
            SelectedRoles = new ObservableCollection<int> { BitrixTaskRole.ResponsibleId, BitrixTaskRole.AccompliceId };
        }
    }
}
