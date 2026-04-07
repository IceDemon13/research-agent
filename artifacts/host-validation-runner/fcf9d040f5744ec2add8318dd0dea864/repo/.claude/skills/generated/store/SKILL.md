---
name: store
description: "Skill for the Store area of telemart_soft_test. 195 symbols across 96 files."
---

# Store

195 symbols | 96 files | Cohesion: 90%

## When to Use

- Working with code in `src/`
- Understanding how WorkSchedulesViewModel, TradeInSegmentsViewModel, TradeInsViewModel work
- Modifying store-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Store/StoreOrdersPackViewModel.cs` | StoreOrdersPackViewModel, HandleLoadedAsync, GetFilteringItem, MapOrder, OnOrderMessage (+18) |
| `src/client/Telemart.Client/ViewModels/Store/StorePurchasesViewModel.cs` | StorePurchasesViewModel, GetPurchaseSaveSource, SetPurchasesSourceAsync, SetNoneSource, RemovePurchaseSource (+14) |
| `src/client/Telemart.Client/ViewModels/Store/StoreInvoicesViewModel.cs` | StoreInvoicesViewModel, RefreshAsync, MapInvoice, OnInvoiceMessage, MassInvoiceAcceptAsync (+6) |
| `src/client/Telemart.Client/ViewModels/Store/StoreOrdersViewModel.cs` | StoreOrdersViewModel, EditContractorAsync, EditPaymentAsync, SetCustomerAsync, SetManagerAsync (+5) |
| `src/client/Telemart.Client/ViewModels/Store/StoreOrdersFilterViewModel.cs` | RefreshAsync, RefreshCitiesAsync, RefreshContractorsAsync, RefreshLegalEntitiesAsync, RefreshOrderSourcesAsync (+2) |
| `src/client/Telemart.Client/ViewModels/Store/ProductsComparisonViewModelBase.cs` | AddOrEditProduct, ChooseProduct, GetChooseProductItems, RecognizeBarcodeViewModelOnFinished, ResetQuantityReal (+2) |
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | ReconfirmOrderAsync, EditReceivedDateAsync, PaymentControlAsync, PrintAssemblyAsync |
| `src/client/Telemart.Client/ViewModels/Store/MassInvoiceProductsComparisonViewModel.cs` | HandleLoadedAsync, HandleOkAsync, GetDeviation, GetDistributedProductsOnInvoiceProducts |
| `src/client/Telemart.Client/ViewModels/Store/ProductScanSerialsViewModel.cs` | AddSerialNumber, AddSerialNumbersToSerials, AddSerialNumberToSerials, ValidateSerialNumber |
| `src/client/Telemart.Client/ViewModels/Store/NpScanSheetViewModel.cs` | HandleLoadedAsync, RefreshEmployeesAsync, RefreshWarehousesAsync, GetSummaryItems |

## Entry Points

Start here when exploring this area:

- **`WorkSchedulesViewModel`** (Class) — `src/client/Telemart.Client/ViewModels/WorkSchedule/WorkSchedulesViewModel.cs:25`
- **`TradeInSegmentsViewModel`** (Class) — `src/client/Telemart.Client/ViewModels/TradeInSegment/TradeInSegmentsViewModel.cs:26`
- **`TradeInsViewModel`** (Class) — `src/client/Telemart.Client/ViewModels/TradeIn/TradeInsViewModel.cs:40`
- **`TasksViewModel`** (Class) — `src/client/Telemart.Client/ViewModels/Tasks/TasksViewModel.cs:27`
- **`SupplierCurrenciesViewModel`** (Class) — `src/client/Telemart.Client/ViewModels/SupplierCurrency/SupplierCurrenciesViewModel.cs:26`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `WorkSchedulesViewModel` | Class | `src/client/Telemart.Client/ViewModels/WorkSchedule/WorkSchedulesViewModel.cs` | 25 |
| `TradeInSegmentsViewModel` | Class | `src/client/Telemart.Client/ViewModels/TradeInSegment/TradeInSegmentsViewModel.cs` | 26 |
| `TradeInsViewModel` | Class | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInsViewModel.cs` | 40 |
| `TasksViewModel` | Class | `src/client/Telemart.Client/ViewModels/Tasks/TasksViewModel.cs` | 27 |
| `SupplierCurrenciesViewModel` | Class | `src/client/Telemart.Client/ViewModels/SupplierCurrency/SupplierCurrenciesViewModel.cs` | 26 |
| `SupplierBillsViewModel` | Class | `src/client/Telemart.Client/ViewModels/SupplierBill/SupplierBillsViewModel.cs` | 27 |
| `StorePurchasesViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/StorePurchasesViewModel.cs` | 41 |
| `StoreInvoicesViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/StoreInvoicesViewModel.cs` | 38 |
| `ShowcasesViewModel` | Class | `src/client/Telemart.Client/ViewModels/Showcase/ShowcasesViewModel.cs` | 36 |
| `AutoShowcasesViewModel` | Class | `src/client/Telemart.Client/ViewModels/Showcase/AutoShowcasesViewModel.cs` | 52 |
| `SegmentsViewModel` | Class | `src/client/Telemart.Client/ViewModels/Segment/SegmentsViewModel.cs` | 23 |
| `RobotPropertiesViewModel` | Class | `src/client/Telemart.Client/ViewModels/RobotProperties/RobotPropertiesViewModel.cs` | 20 |
| `PromoCodesViewModel` | Class | `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodesViewModel.cs` | 31 |
| `ProductCompatibilitiesViewModel` | Class | `src/client/Telemart.Client/ViewModels/ProductCompatibility/ProductCompatibilitiesViewModel.cs` | 29 |
| `PaymentsViewModel` | Class | `src/client/Telemart.Client/ViewModels/Payments/PaymentsViewModel.cs` | 21 |
| `NotificationsViewModel` | Class | `src/client/Telemart.Client/ViewModels/Notification/NotificationsViewModel.cs` | 26 |
| `LogisticsAnaliticsViewModel` | Class | `src/client/Telemart.Client/ViewModels/LogisticsAnalitics/LogisticsAnaliticsViewModel.cs` | 30 |
| `LocationsViewModel` | Class | `src/client/Telemart.Client/ViewModels/Locations/LocationsViewModel.cs` | 22 |
| `DistrictsViewModel` | Class | `src/client/Telemart.Client/ViewModels/District/DistrictsViewModel.cs` | 19 |
| `DiscussionsViewModel` | Class | `src/client/Telemart.Client/ViewModels/Discussions/DiscussionsViewModel.cs` | 24 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `HandleLoadedAsync → SetCopyTitle` | cross_community | 4 |
| `HandleLoadedAsync → BeforeSetData` | cross_community | 4 |
| `HandleLoadedAsync → Update` | cross_community | 4 |
| `HandleLoadedAsync → AfterSetData` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| ServiceRequests | 6 calls |
| Order | 6 calls |
| Business | 2 calls |
| ServiceRepairs | 1 calls |
| Purchase | 1 calls |
| Diagnostics | 1 calls |
| TelemartAnimatedGif | 1 calls |
| Invoice | 1 calls |

## How to Explore

1. `gitnexus_context({name: "WorkSchedulesViewModel"})` — see callers and callees
2. `gitnexus_query({query: "store"})` — find related execution flows
3. Read key files listed above for implementation details
