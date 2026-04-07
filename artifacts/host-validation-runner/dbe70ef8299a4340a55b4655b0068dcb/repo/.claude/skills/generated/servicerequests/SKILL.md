---
name: servicerequests
description: "Skill for the ServiceRequests area of telemart_soft_test. 139 symbols across 64 files."
---

# ServiceRequests

139 symbols | 64 files | Cohesion: 62%

## When to Use

- Working with code in `src/`
- Understanding how RequisitesViewItem, AssembledComputersFilteringItem, DiagnoseServiceRequestModel work
- Modifying servicerequests-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/ServiceRequestViewModel.cs` | CloseDiscussionAsync, CompleteRequestAsync, RemoveDocumentAsync, ReopenRequestAsync, ResetRequestAsync (+28) |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/ServiceRequestsViewModel.cs` | ShowValidationResultView, SplitRequestsAsync, JoinRequestsAsync, RefreshAsync, RefreshCitiesAsync (+2) |
| `src/client/Telemart.Client/ViewModels/Service/ServiceProducts/ServiceProductViewModel.cs` | GiveBackToSupplierAsync, UtilizeAsync, GiveOnRepairAsync, SupplierRejectAsync, ChangeDecisionAsync |
| `src/client/Telemart.Client/ViewModels/Money/Refund/RefundViewModel.cs` | ConfirmAsync, RevertAsync, CancelAsync, PrintStatementReturnRefundAsync, SetRequisites |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/ChangeRequirementViewModel.cs` | ReturnMoneyPaymentTypeChanged, CalculateRequisitesEnabled, HandleLoadedAsync, HandleOkAsync, MapToDto |
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | RemoveDocumentAsync, ShowModuleAnalyticsViewAsync, SelectedPaymentChangedCallback |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/TakeServiceRequestViewModel.cs` | HandleOkAsync, HandleLoadedAsync, RefreshWarehousesAsync |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRepairs/ServiceRepairViewModel.cs` | CancelRepairAsync, ConfirmAsync, ReconfirmAsync |
| `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/StoreReturnInvoicesViewModel.cs` | PrintSnAsync, PrintReturnInvoiceAssemblyAsync, PrintReturnInvoiceSupplierAsync |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/CompensationHelper.cs` | GetCompensationPrices, GetProssibleCompensationPricesAsync, ConvertPriceAsync |

## Entry Points

Start here when exploring this area:

- **`RequisitesViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Common/RequisitesViewItem.cs:8`
- **`AssembledComputersFilteringItem`** (Class) — `src/client/Telemart.Client/ViewModels/AssemblyService/AssembledComputersFilteringItem.cs:4`
- **`DiagnoseServiceRequestModel`** (Class) — `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Diagnostics/DiagnoseServiceRequestModel.cs:12`
- **`IsProductRemovedDto`** (Class) — `src/client/Telemart.Client.TransferObjects/IsProductRemovedDto.cs:4`
- **`CreateServiceRequestTrackNumberSourceViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/CreateServiceRequestTrackNumberSourceViewItem.cs:10`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `RequisitesViewItem` | Class | `src/client/Telemart.Client/ViewModels/Common/RequisitesViewItem.cs` | 8 |
| `AssembledComputersFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/AssemblyService/AssembledComputersFilteringItem.cs` | 4 |
| `DiagnoseServiceRequestModel` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Diagnostics/DiagnoseServiceRequestModel.cs` | 12 |
| `IsProductRemovedDto` | Class | `src/client/Telemart.Client.TransferObjects/IsProductRemovedDto.cs` | 4 |
| `CreateServiceRequestTrackNumberSourceViewItem` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/CreateServiceRequestTrackNumberSourceViewItem.cs` | 10 |
| `LoadAsync` | Method | `src/client/Telemart.Client/Dictionaries/Dictionaries.cs` | 237 |
| `Handle` | Method | `src/client/Telemart.Client/Mediator/RequestsHandlers/PrintWarrantyCardRequestHandler.cs` | 51 |
| `Handle` | Method | `src/client/Telemart.Client/Mediator/RequestsHandlers/PrintOrderDocumentHandler.cs` | 48 |
| `PrintAsync` | Method | `src/client/Telemart.Client/Business/Delivery/ScanSheets/TelemartScanSheetProcessor.cs` | 54 |
| `ReturnMoneyPaymentTypeChanged` | Method | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/CreateManyServiceRequestModel.cs` | 340 |
| `IsCachlessPayment` | Method | `src/client/Telemart.Client.Dictionaries/Payment.cs` | 116 |
| `GetServiceRequestPaymentTypes` | Method | `src/client/Telemart.Client/Dictionaries/Dictionaries.cs` | 130 |
| `OnGoForward` | Method | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/CreateManyServiceRequestStep5ViewModel.cs` | 138 |
| `GetCompensationPrices` | Method | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/CompensationHelper.cs` | 21 |
| `GetProssibleCompensationPricesAsync` | Method | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/CompensationHelper.cs` | 48 |
| `DeleteAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeInSegment/TradeInSegmentsViewModel.cs` | 285 |
| `RemoveDocumentAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | 1298 |
| `DeleteAsync` | Method | `src/client/Telemart.Client/ViewModels/Segment/SegmentsViewModel.cs` | 236 |
| `DeleteAsync` | Method | `src/client/Telemart.Client/ViewModels/Reporting/ReportSelectionViewModel.cs` | 158 |
| `RemoveLayoutAsync` | Method | `src/client/Telemart.Client/ViewModels/Reporting/ReportLayoutsViewModel.cs` | 170 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `DiagnoseRequestAsync → CreateEntityMessage` | cross_community | 5 |
| `DiagnoseRequestAsync → LockEntityAsync` | cross_community | 4 |
| `DiagnoseRequestAsync → UnlockEntityAsync` | cross_community | 4 |
| `DiagnoseRequestAsync → PropertyGridRowPropertyDescriptor` | cross_community | 4 |
| `HandleOkAsync → GetReturnMoneyString` | cross_community | 4 |
| `GiveAsync → QueryCarriesAsync` | cross_community | 4 |
| `SentCheckAsync → QueryCarriesAsync` | cross_community | 4 |
| `SentCheckAsync → QueryWorkPlaceTypesAsync` | cross_community | 4 |
| `SentCheckAsync → QueryPaymentsAsync` | cross_community | 4 |
| `SentCheckAsync → QueryWarrantiesAsync` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Order | 10 calls |
| Telemart.Client.Dictionaries | 2 calls |
| Create | 2 calls |
| Refund | 1 calls |
| CreateScanSheetForEntities | 1 calls |
| Base | 1 calls |
| Movement | 1 calls |
| ViewModels | 1 calls |

## How to Explore

1. `gitnexus_context({name: "RequisitesViewItem"})` — see callers and callees
2. `gitnexus_query({query: "servicerequests"})` — find related execution flows
3. Read key files listed above for implementation details
