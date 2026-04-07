using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.SupplierBill
{
    public class SupplierBillFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<ContractorDto> contractorsList;

        public SupplierBillFilterViewModel(IWebClient webClient, IDictionaries dictionaries)
        {
            WebClient = webClient;

            States = dictionaries.GetItems<SupplierBillState>().ToReadOnlyObservableCollection();
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Suppliers
        {
            get { return GetProperty(() => Suppliers); }
            set { SetProperty(() => Suppliers, value); }
        }

        public ReadOnlyObservableCollection<SupplierBillState> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public string BillIds
        {
            get { return GetProperty(() => BillIds); }
            set { SetProperty(() => BillIds, value); }
        }

        public string Number
        {
            get { return GetProperty(() => Number); }
            set { SetProperty(() => Number, value); }
        }

        public string InvoiceIds
        {
            get { return GetProperty(() => InvoiceIds); }
            set { SetProperty(() => InvoiceIds, value); }
        }

        public List<object> SelectedSuppliers
        {
            get { return GetProperty(() => SelectedSuppliers); }
            set { SetProperty(() => SelectedSuppliers, value); }
        }

        public List<object> SelectedStates
        {
            get { return GetProperty(() => SelectedStates); }
            set { SetProperty(() => SelectedStates, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IWebClient WebClient { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<SupplierBillFilterViewModel> builder)
        {
            builder.Property(x => x.BillIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
            builder.Property(x => x.InvoiceIds)
               .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public SupplierBillFilteringItem GetFilteringItem()
        {
            return new SupplierBillFilteringItem(BillIds, InvoiceIds, Number, SelectedSuppliers?.Cast<int>().ToList(), SelectedStates?.Cast<int>().ToList());
        }

        public Task RefreshAsync()
        {
            return RefreshSuppliersAsync();
        }

        public void ResetFilterValues()
        {
            BillIds = null;
            InvoiceIds = null;
            Number = null;
            SelectedSuppliers = null;
            SelectedStates = new List<object> { SupplierBillState.New.Id };
        }

        private async Task RefreshSuppliersAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(contractors, contractorsList))
            {
                return;
            }

            contractorsList = contractors;

            Suppliers = contractorsList
                .Where(x => x.Active && x.IsSupplier)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();
        }
    }
}
