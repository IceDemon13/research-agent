---
name: order
description: "Skill for the Order area of telemart_soft_test. 552 symbols across 227 files."
---

# Order

552 symbols | 227 files | Cohesion: 61%

## When to Use

- Working with code in `src/`
- Understanding how OrderProductDto, OnOrderCreationFinishedMessage, ProductSimpleDto work
- Modifying order-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | CopyCreditDataAsync, CancelOrderBillAsync, RecalculateOrderBillAsync, QueryExternalPaymentStateAsync, SetExternalOrderAsync (+160) |
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderPackViewModel.cs` | PrintBillInvoiceAsync, PrintBillAsync, PackInternalAsync, FastGiveAsync, GetOrderDeliveryDto (+7) |
| `src/client/Telemart.Client/Business/Order/OrderReportBuilder.cs` | BuildAcceptanceProtocolReportAsync, BuildChequeReportAsync, BuildTapeChequeReportAsync, GetReportAsync, GetProducts (+7) |
| `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | RemoveContractorContactAsync, EditContractorClassAsync, RefreshSupplierCategoriesAbcAsync, DeleteSupplierCategoryAbcAsync, SetAsMainCarryAsync (+3) |
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderProductViewModel.cs` | IsAssemblyOrAssembledComputerRule, DeleteAssemblyProducts, RefreshAllowEdit, CurrencyOutIdChangedCallback, QuantityChangedCallback (+3) |
| `src/client/Telemart.Client/ViewModels/SupplierBill/SupplierBillViewModel.cs` | AddDocument, RemoveDocumentAsync, ProcessBillAsync, CancelBillAsync, CancelCompletedAsync (+2) |
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderConfirmViewModel.cs` | HandleOkAsync, GetOrderProducts, GetOrderPayments, OrderConfirmViewModel, HandleLoadedAsync (+2) |
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderEditDeliveryDateViewModel.cs` | HandleLoadedAsync, OneOrderAsync, ManyOrdersAsync, GetOrderDeliveryTimeAsync, HandleOkAsync (+2) |
| `src/client/Telemart.Client/ViewModels/Store/Order/MoveProductViewModel.cs` | HandleOkAsync, CreateOrderAsync, HandleLoadedAsync, ValidateOrder, GetFilteringItem (+1) |
| `src/client/Telemart.Client/ViewModels/Store/StoreOrdersViewModel.cs` | PrintChequeAsync, PrintOrderOnFiscalRegistrarAsync, StopCancelingOrderFromSiteAsync, SetCarryAsync, PrintActIncomeAsync (+1) |

## Entry Points

Start here when exploring this area:

- **`OrderProductDto`** (Class) — `src/client/Telemart.Client.TransferObjects/OrderProductDto.cs:6`
- **`OnOrderCreationFinishedMessage`** (Class) — `src/client/Telemart.Client/Common/Messages/OnOrderCreationFinishedMessage.cs:2`
- **`ProductSimpleDto`** (Class) — `src/client/Telemart.Client.TransferObjects/ProductSimpleDto.cs:6`
- **`OrderSaveDto`** (Class) — `src/client/Telemart.Client.TransferObjects/OrderSaveDto.cs:5`
- **`OrderCreatePackedDto`** (Class) — `src/client/Telemart.Client.TransferObjects/OrderCreatePackedDto.cs:5`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `OrderProductDto` | Class | `src/client/Telemart.Client.TransferObjects/OrderProductDto.cs` | 6 |
| `OnOrderCreationFinishedMessage` | Class | `src/client/Telemart.Client/Common/Messages/OnOrderCreationFinishedMessage.cs` | 2 |
| `ProductSimpleDto` | Class | `src/client/Telemart.Client.TransferObjects/ProductSimpleDto.cs` | 6 |
| `OrderSaveDto` | Class | `src/client/Telemart.Client.TransferObjects/OrderSaveDto.cs` | 5 |
| `OrderCreatePackedDto` | Class | `src/client/Telemart.Client.TransferObjects/OrderCreatePackedDto.cs` | 5 |
| `OrderCreateDto` | Class | `src/client/Telemart.Client.TransferObjects/OrderCreateDto.cs` | 5 |
| `TaskFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Tasks/TaskFilteringItem.cs` | 5 |
| `EventFilteringItem` | Class | `src/client/Telemart.Client.Data/Requests/Features/Event/EventFilteringItem.cs` | 4 |
| `AssemblyServicesFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/AssemblyService/AssemblyServicesFilteringItem.cs` | 5 |
| `OrderProductQuantityViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/OrderProductQuantityViewItem.cs` | 4 |
| `ValidatableItem` | Class | `src/client/Telemart.Client/Common/MvvmEnhancements/ValidatableItem.cs` | 4 |
| `OrderSourceTypeViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/OrderSourceTypeViewItem.cs` | 4 |
| `OrderPaymentViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/OrderPaymentViewItem.cs` | 4 |
| `OrderCarryTypeViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/OrderCarryTypeViewItem.cs` | 4 |
| `OrderBonusSaveDto` | Class | `src/client/Telemart.Client.TransferObjects/OrderBonusSaveDto.cs` | 4 |
| `ScheduleDeliveryViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/ScheduleDeliveryViewItem.cs` | 7 |
| `OrderViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | 164 |
| `OrderLogisticsViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/OrderLogisticsViewModel.cs` | 18 |
| `OrderConfirmViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/OrderConfirmViewModel.cs` | 30 |
| `OrderEditPriceViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/OrderEditPrice/OrderEditPriceViewItem.cs` | 8 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `HandleOkAsync → ValidationResultViewModelParameter` | cross_community | 5 |
| `GiveAsync → QueryCarriesAsync` | cross_community | 4 |
| `EquipmentSettingsWorkAsync → ValidateAsync` | cross_community | 4 |
| `SentCheckAsync → QueryCarriesAsync` | cross_community | 4 |
| `SentCheckAsync → QueryWorkPlaceTypesAsync` | cross_community | 4 |
| `SentCheckAsync → QueryPaymentsAsync` | cross_community | 4 |
| `SentCheckAsync → QueryWarrantiesAsync` | cross_community | 4 |
| `SentCheckAsync → ExecuteApiRequestAsync` | cross_community | 4 |
| `SentCheckAsync → QueryCashboxes` | cross_community | 4 |
| `SentCheckAsync → QueryLegalEntities` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| ServiceRequests | 52 calls |
| TradeIn | 36 calls |
| Telemart.Client.Dictionaries | 7 calls |
| Store | 5 calls |
| ServiceRepairs | 5 calls |
| Telemart.Client.PosTerminal.Ingenico | 4 calls |
| Create | 4 calls |
| Telemart.Client.TransferObjects | 3 calls |

## How to Explore

1. `gitnexus_context({name: "OrderProductDto"})` — see callers and callees
2. `gitnexus_query({query: "order"})` — find related execution flows
3. Read key files listed above for implementation details
