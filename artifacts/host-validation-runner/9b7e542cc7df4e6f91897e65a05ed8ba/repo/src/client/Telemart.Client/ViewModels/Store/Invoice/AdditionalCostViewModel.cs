using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class AdditionalCostViewModel : TelemartEditorViewModelBase<InvoiceAdditionalCostDto, AdditionalCostParameter, InvoiceAdditionalCostViewItem>
    {
        private InvoiceDto invoice;

        public AdditionalCostViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            LockSupport = false;
        }

        public ReadOnlyObservableCollection<InvoiceAdditionalCostType> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ReadOnlyObservableCollection<InvoiceAdditionalCostSource> Sources
        {
            get { return GetProperty(() => Sources); }
            private set { SetProperty(() => Sources, value); }
        }

        public ReadOnlyObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        public ReadOnlyObservableCollection<InvoiceAdditionalCostProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        protected override string CreatedActionMessage { get; } = "создан";

        protected override string EntityName { get; } = "Доп. расход";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        protected override Task<Result<InvoiceAdditionalCostDto>> CreateEntityAsync()
        {
            InvoiceAdditionalCostCreateDto createDto = new InvoiceAdditionalCostCreateDto
            {
                InvoiceId = Model.InvoiceId,
                Amount = Model.Amount.Value,
                CurrencyId = Model.CurrencyId.Value,
                TypeId = Model.TypeId.Value,
                SourceId = Model.SourceId.Value,
                Products = Model.Products
                .Where(x => x.Include)
                .Select(x => new InvoiceAdditionalCostCreateProductDto
                {
                    InvoiceProductId = x.InvoiceProductId,
                    Quantity = x.Quantity
                }).ToArray()
            };

            return WebClient.ExecuteApiRequestAsync(new CreateInvoiceAdditionalCost(createDto));
        }

        protected override Task<Result<InvoiceAdditionalCostDto>> UpdateEntityAsync()
        {
            InvoiceAdditionalCostSaveDto saveDto = new InvoiceAdditionalCostSaveDto
            {
                Id = Model.Id,
                Amount = Model.Amount.Value,
                CurrencyId = Model.CurrencyId.Value,
                TypeId = Model.TypeId.Value,
                SourceId = Model.SourceId.Value,
                Products = Model.Products
                .Where(x => x.Include)
                .Select(x => new InvoiceAdditionalCostSaveProductDto
                {
                    Id = x.Id,
                    InvoiceProductId = x.InvoiceProductId,
                    Quantity = x.Quantity
                }).ToArray()
            };

            return WebClient.ExecuteApiRequestAsync(new UpdateInvoiceAdditionalCost(saveDto));
        }

        protected override Task<InvoiceAdditionalCostDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryInvoiceAdditionalCost(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            AdditionalCostParameter parameter = (AdditionalCostParameter)Parameter;

            invoice = await WebClient.ExecuteApiRequestAsync(new QueryInvoice(parameter.InvoiceId));

            Sources = Dictionaries.GetItems<InvoiceAdditionalCostSource>().ToReadOnlyObservableCollection();
            Types = Dictionaries.GetItems<InvoiceAdditionalCostType>().ToReadOnlyObservableCollection();
            Currencies = Dictionaries.GetItems<Currency>().ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();
        }

        protected override void BeforeSetData(InvoiceAdditionalCostViewItem model, object dto)
        {
            base.BeforeSetData(model, dto);

            if (model.Id == 0)
            {
                model.Products = invoice.InvoiceProducts
                    .Select(x => new InvoiceAdditionalCostProductViewItem
                    {
                        InvoiceProductId = x.Id,
                        ProductId = x.ProductId,
                        ProductName = x.GetLocalName(LocalizableNameType.Ukr),
                        Quantity = x.Quantity,
                        Include = true
                    })
                    .ToObservableCollection();
            }
            else
            {
                IReadOnlyDictionary<int, int> invoiceProductQuantities = model.Products
                    .ToDictionary(x => x.InvoiceProductId, x => x.Quantity);

                model.Products = invoice.InvoiceProducts
                    .Select(x => new InvoiceAdditionalCostProductViewItem
                    {
                        InvoiceProductId = x.Id,
                        ProductId = x.ProductId,
                        ProductName = x.GetLocalName(LocalizableNameType.Ukr),
                        Quantity = invoiceProductQuantities.GetValueOrDefault(x.Id, x.Quantity),
                        Include = invoiceProductQuantities.ContainsKey(x.Id)
                    })
                    .ToObservableCollection();
            }
        }

        protected override Task<LockResponse<InvoiceAdditionalCostDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<InvoiceAdditionalCostDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание доп. расхода";
        }

        protected override void SetEditTitle()
        {
            Title = $"Изменение доп. расхода №{Model.Id}";
        }
    }
}