---
name: base
description: "Skill for the Base area of telemart_soft_test. 60 symbols across 40 files."
---

# Base

60 symbols | 40 files | Cohesion: 78%

## When to Use

- Working with code in `src/`
- Understanding how CallFinishMessage, InvoiceAdditionalCostProductViewItem, ServiceRequestViewItem work
- Modifying base-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Base/TelemartEditorViewModelBase.cs` | OnClose, IsValid, SaveAsync, SetEditTitle, BeforeSetData (+6) |
| `src/client/Telemart.Client/ViewModels/Base/TelemartViewModelBase.cs` | HandleUnloadedAsync, OnHandleLoadedFinished, GetUnexpectedStatusErrorMessage, HandleLoadedInternalAsync, HandleUnloadedInternalAsync |
| `src/client/Telemart.Client.Data/HubClient/Base/HubClientBase.cs` | StartAsync, SendAsync, ExecuteAsync |
| `src/client/Telemart.Client/ViewModels/Base/TelemartDialogViewModelBase.cs` | OnClose, OnDestroy |
| `src/client/Telemart.Client/ViewModels/Store/Call/OutcomingCallViewModel.cs` | OnClose, OnDestroy |
| `src/client/Telemart.Client/ViewModels/AdditionalServiceProduct/AdditionalServiceProductViewModel.cs` | GetEntityAsync, SaveAsync |
| `src/client/Telemart.Client.Data/Requests/Base/QueryEntitiesPagedRequestBase.cs` | BuildRequest, GetIntValue |
| `src/client/Telemart.Client/ViewModels/TradeInSegment/TradeInSegmentCategorySettingsViewModel.cs` | OnClose |
| `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseCategoryViewModel.cs` | OnClose |
| `src/client/Telemart.Client/ViewModels/Segment/SegmentCategorySettingsViewModel.cs` | OnClose |

## Entry Points

Start here when exploring this area:

- **`CallFinishMessage`** (Class) — `src/client/Telemart.Client/Common/Messages/CallFinishMessage.cs:2`
- **`InvoiceAdditionalCostProductViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceAdditionalCostProductViewItem.cs:4`
- **`ServiceRequestViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/ServiceRequestViewItem.cs:21`
- **`ServiceRepairViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Service/ServiceRepairs/ServiceRepairViewItem.cs:13`
- **`ReportPrinterBase`** (Class) — `src/client/Telemart.Client/Reports/ReportBuilders/Base/ReportPrinterBase.cs:8`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `CallFinishMessage` | Class | `src/client/Telemart.Client/Common/Messages/CallFinishMessage.cs` | 2 |
| `InvoiceAdditionalCostProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceAdditionalCostProductViewItem.cs` | 4 |
| `ServiceRequestViewItem` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/ServiceRequestViewItem.cs` | 21 |
| `ServiceRepairViewItem` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRepairs/ServiceRepairViewItem.cs` | 13 |
| `ReportPrinterBase` | Class | `src/client/Telemart.Client/Reports/ReportBuilders/Base/ReportPrinterBase.cs` | 8 |
| `AssemblyServicePassportReportPrinter` | Class | `src/client/Telemart.Client/Reports/ReportBuilders/AssemblyService/PassportReport/AssemblyServicePassportReportPrinter.cs` | 20 |
| `ICommentEntity` | Interface | `src/client/Telemart.Client/ViewModels/Base/ICommentEntity.cs` | 2 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/TradeInSegment/TradeInSegmentCategorySettingsViewModel.cs` | 92 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseCategoryViewModel.cs` | 119 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Segment/SegmentCategorySettingsViewModel.cs` | 92 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/ModuleAnalytics/ModuleAnalyticsViewModel.cs` | 129 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Base/TelemartEditorViewModelBase.cs` | 128 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Base/TelemartDialogViewModelBase.cs` | 80 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/OrderAutoConfirmSettingsViewModel.cs` | 105 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 353 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/OutcomingCallViewModel.cs` | 326 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Content/ProductFeatureGroups/FeatureContractorParserSourcesViewModel.cs` | 74 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 303 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Diagnostics/ServiceRequestNomenclatureSeriesViewModel.cs` | 122 |
| `OpenByMessage` | Method | `src/client/Telemart.Client/Helpers/ViewModelOpenHelper.cs` | 10 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `PrintAsync → GetIntValue` | cross_community | 5 |
| `PrintAsync → SkipTakeUrlParameters` | cross_community | 5 |
| `PrintAsync → BuildRequest` | cross_community | 5 |
| `DiagnoseRequestAsync → CreateEntityMessage` | cross_community | 5 |
| `AuthenticateAsync → BuildRequest` | cross_community | 5 |
| `TakeRequestAsync → CreateEntityMessage` | intra_community | 5 |
| `QueryAndSetAuthenticatedEmployeeAsync → BuildRequest` | cross_community | 5 |
| `QueryPricesByIdsAsync → BuildRequest` | cross_community | 5 |
| `QuerySimplePricesByIdsAsync → BuildRequest` | cross_community | 5 |
| `SavePricesAsync → BuildRequest` | cross_community | 5 |

## Connected Areas

| Area | Connections |
|------|-------------|
| ServiceRequests | 12 calls |
| Controls | 2 calls |
| Call | 1 calls |
| Order | 1 calls |
| RobotProperties | 1 calls |
| Locations | 1 calls |

## How to Explore

1. `gitnexus_context({name: "CallFinishMessage"})` — see callers and callees
2. `gitnexus_query({query: "base"})` — find related execution flows
3. Read key files listed above for implementation details
