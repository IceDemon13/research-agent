using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.ViewModels.SupplierCurrency
{
    public class SupplierCurrenciesFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<ContractorDto> _suppliers;

        public SupplierCurrenciesFilterViewModel(IWebClient webClient)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));

            DateAfter = DateTime.Now.AddDays(-7);
            DateBefore = DateTime.Now;
        }

        public DateTime? DateAfter
        {
            get { return GetProperty(() => DateAfter); }
            set { SetProperty(() => DateAfter, value); }
        }

        public DateTime? DateBefore
        {
            get { return GetProperty(() => DateBefore); }
            set { SetProperty(() => DateBefore, value); }
        }

        public List<object> SelectedSuppliers
        {
            get { return GetProperty(() => SelectedSuppliers); }
            set { SetProperty(() => SelectedSuppliers, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> Suppliers { get; } = new ObservableRangeCollection<ComboBoxItem>();

        private IWebClient WebClient { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        string IDataErrorInfo.Error => string.Empty;

        public static void BuildMetadata(MetadataBuilder<SupplierCurrenciesFilterViewModel> builder)
        {
            builder.Property(x => x.DateAfter)
                .MatchesInstanceRule((x, y) => !x.HasValue || x <= y.DateBefore, () => "Дата от должна быть меньше даты до");
            builder.Property(x => x.DateBefore)
                .MatchesInstanceRule((x, y) => !x.HasValue || x > y.DateAfter, () => "Дата до доллжна быть больше даты от");
        }

        public SupplierCurrencyFilterItem GetSupplierCurrenciesFilteringItem()
        {
            SupplierCurrencyFilterItem item = new SupplierCurrencyFilterItem()
            {
                DateFrom = DateAfter,
                DateTo = DateBefore,
                SupplierIds = SelectedSuppliers?.Select(x => ((ComboBoxItem)x).Id).ToList()
            };

            return item;
        }

        public Task RefreshAsync()
        {
            return RefreshSupplierssAsync();
        }

        public void ResetFilterValues()
        {
            DateAfter = DateTime.Now.AddDays(-7);
            DateBefore = DateTime.Now;
            SelectedSuppliers = null;
        }

        private async Task RefreshSupplierssAsync()
        {
            if (_suppliers == null)
            {
                PagedResult<ContractorDto> contractorsResult = await WebClient.ExecuteApiRequestAsync(new QueryContractors(true));

                if (contractorsResult?.Data != null)
                {
                    _suppliers = contractorsResult.Data
                        .Where(x => x.IsSupplier && !x.IsFolder)
                        .OrderBy(x => x.Name)
                        .ToList();

                    Suppliers.Clear();

                    Suppliers.AddRange(_suppliers?.Select(x => new ComboBoxItem(x.Id, x.Name)));
                }
            }
        }
    }
}