---
name: webclient
description: "Skill for the WebClient area of telemart_soft_test. 52 symbols across 35 files."
---

# WebClient

52 symbols | 35 files | Cohesion: 62%

## When to Use

- Working with code in `src/`
- Understanding how ModifiedOnAfterFilteringItem, FilteringItemBase, TradeInFilteringItem work
- Modifying webclient-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/WebClient/TelemartWebClient.cs` | ExecuteApiRequestAsBytesAsync, AuthenticateAsync, QueryAndSetAuthenticatedEmployeeAsync, SetAuthenticatedEmployee, HandleErrorsAsync (+7) |
| `src/client/Telemart.Client/Helpers/OrderGiveHelper.cs` | FastGiveAsync, PrintBillAsync, PrintBillInvoiceAsync |
| `src/client/Telemart.Client/Common/Services/MessageFacadeServive.cs` | ShowNotificationWarning, ShowMessageBox, ShowMessageBoxError |
| `src/client/Telemart.Client.Data/WebClient/FilteringItemBase.cs` | FilteringItemBase, WithInitializedDataViaReflection |
| `src/client/Telemart.Client/ViewModels/Store/StoreOrdersFilterViewModel.cs` | StoreOrdersFilterViewModel, SetFilteringItem |
| `src/client/Telemart.Client.Data/WebClient/ModifiedOnAfterFilteringItem.cs` | ModifiedOnAfterFilteringItem |
| `src/client/Telemart.Client/ViewModels/TradeIn/TradeInFilteringItem.cs` | TradeInFilteringItem |
| `src/client/Telemart.Client/ViewModels/Novaposhta/CourierCallFilteringItem.cs` | CourierCallFilteringItem |
| `src/client/Telemart.Client/ViewModels/Notification/NotificationsFilteringItem.cs` | NotificationsFilteringItem |
| `src/client/Telemart.Client/ViewModels/LogisticsAnalitics/LogisticsAnaliticsFilteringItem.cs` | LogisticsAnaliticsFilteringItem |

## Entry Points

Start here when exploring this area:

- **`ModifiedOnAfterFilteringItem`** (Class) — `src/client/Telemart.Client.Data/WebClient/ModifiedOnAfterFilteringItem.cs:4`
- **`FilteringItemBase`** (Class) — `src/client/Telemart.Client.Data/WebClient/FilteringItemBase.cs:9`
- **`TradeInFilteringItem`** (Class) — `src/client/Telemart.Client/ViewModels/TradeIn/TradeInFilteringItem.cs:5`
- **`CourierCallFilteringItem`** (Class) — `src/client/Telemart.Client/ViewModels/Novaposhta/CourierCallFilteringItem.cs:5`
- **`NotificationsFilteringItem`** (Class) — `src/client/Telemart.Client/ViewModels/Notification/NotificationsFilteringItem.cs:5`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `ModifiedOnAfterFilteringItem` | Class | `src/client/Telemart.Client.Data/WebClient/ModifiedOnAfterFilteringItem.cs` | 4 |
| `FilteringItemBase` | Class | `src/client/Telemart.Client.Data/WebClient/FilteringItemBase.cs` | 9 |
| `TradeInFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInFilteringItem.cs` | 5 |
| `CourierCallFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Novaposhta/CourierCallFilteringItem.cs` | 5 |
| `NotificationsFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Notification/NotificationsFilteringItem.cs` | 5 |
| `LogisticsAnaliticsFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/LogisticsAnalitics/LogisticsAnaliticsFilteringItem.cs` | 4 |
| `DiscussionsFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Discussions/DiscussionsFilteringItem.cs` | 5 |
| `ReturnInvoiceFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ReturnInvoiceFilteringItem.cs` | 6 |
| `ServiceProductFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceProducts/ServiceProductFilteringItem.cs` | 7 |
| `ServiceRequestsGroupFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/ServiceRequestsGroupFilteringItem.cs` | 4 |
| `RepairFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRepairs/RepairFilteringItem.cs` | 6 |
| `ServiceMovementsFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceMovements/ServiceMovementsFilteringItem.cs` | 6 |
| `ProductCatalogFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Directories/ProductsCatalog/ProductCatalogFilteringItem.cs` | 6 |
| `SearchRequestsViewModel` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRepairs/FormInvoice/SearchRequestsViewModel.cs` | 26 |
| `BankPaymentFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Money/Receive/BankPayment/BankPaymentFilteringItem.cs` | 7 |
| `StoreOrdersFilterViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/StoreOrdersFilterViewModel.cs` | 27 |
| `PromoCodeFilterViewModel` | Class | `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeFilterViewModel.cs` | 16 |
| `AssembledComputerRulesFilterViewModel` | Class | `src/client/Telemart.Client/ViewModels/AssembledComputerRule/AssembledComputerRulesFilterViewModel.cs` | 5 |
| `StoreCallsFilterViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/Call/StoreCallsFilterViewModel.cs` | 21 |
| `PagingInfo` | Class | `src/client/Telemart.Client.TransferObjects/Paging/PagingInfo.cs` | 4 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `PrintAsync → BuildRequest` | cross_community | 6 |
| `RefreshAsync → ShowMessageBoxError` | cross_community | 6 |
| `RefreshAsync → ExecuteAsync` | cross_community | 6 |
| `RefreshAsync → PagingInfo` | cross_community | 6 |
| `RefreshAsync → FetchPageAsync` | cross_community | 6 |
| `PrintAsync → ShowMessageBox` | cross_community | 5 |
| `PrintAsync → JsonSerializer` | cross_community | 5 |
| `PrintAsync → GetIntValue` | cross_community | 5 |
| `PrintAsync → SkipTakeUrlParameters` | cross_community | 5 |
| `PrintAsync → BuildRequest` | cross_community | 5 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Services | 4 calls |
| Telemart.Client.Data | 2 calls |
| Order | 2 calls |
| Telemart.Client.Dictionaries | 1 calls |
| Authentication | 1 calls |
| Base | 1 calls |
| TelemartAnimatedGif | 1 calls |

## How to Explore

1. `gitnexus_context({name: "ModifiedOnAfterFilteringItem"})` — see callers and callees
2. `gitnexus_query({query: "webclient"})` — find related execution flows
3. Read key files listed above for implementation details
