---
name: create
description: "Skill for the Create area of telemart_soft_test. 127 symbols across 77 files."
---

# Create

127 symbols | 77 files | Cohesion: 83%

## When to Use

- Working with code in `src/`
- Understanding how WizardPageViewModelBase, CreateSupplierBillTextPageViewModel, CreateSupplierBillMatchingPageViewModel work
- Modifying create-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/SelectOrderPageViewModel.cs` | SelectOrderPageViewModel, SelectProduct, OnGoForwardInternal, SearchOrders, SetOrder (+2) |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/CreateManyServiceRequestModel.cs` | OnWarehouseInChanged, SetOrderDataAsync, GetPaymentTypes, ClientRequirementChanged, GetSaveDto (+2) |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/CreateServiceRequestModel.cs` | WarehouseInChanged, SetOrderData, SetSerials, SetProducts, GetSaveDto (+1) |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/DeclarantPageViewModel.cs` | DeclarantPageViewModel, HandleLoadedAsync, RefreshContractorsAsync, RefreshWarehousesAsync, RefreshCitiesAsync |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/CreateManyServiceRequestStep3ViewModel.cs` | CreateManyServiceRequestStep3ViewModel, HandleLoadedAsync, RefreshWarehousesAsync, RefreshContractorsAsync, RefreshCitiesAsync |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/CreateManyServiceRequestStep2ViewModel.cs` | CreateManyServiceRequestStep2ViewModel, OnGoForward, MapToServiceRequestProductSnViewItem, MapToServiceRequestProductViewItem, HandleLoadedAsync |
| `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillMatchingPageViewModel.cs` | CreateSupplierBillMatchingPageViewModel, SelectProduct, OnGoForward, ShowValidationResultView |
| `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillTextPageViewModel.cs` | CreateSupplierBillTextPageViewModel, OnGoForward, ShowValidationResultView |
| `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillContractorPageViewModel.cs` | CreateSupplierBillContractorPageViewModel, OnGoForwardInternal, ShowValidationResultView |
| `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillConfirmPageViewModel.cs` | CreateSupplierBillConfirmPageViewModel, OnGoForward, ShowValidationResultView |

## Entry Points

Start here when exploring this area:

- **`WizardPageViewModelBase`** (Class) — `src/client/Telemart.Client/ViewModels/Base/WizardPageViewModelBase.cs:14`
- **`CreateSupplierBillTextPageViewModel`** (Class) — `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillTextPageViewModel.cs:30`
- **`CreateSupplierBillMatchingPageViewModel`** (Class) — `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillMatchingPageViewModel.cs:20`
- **`CreateSupplierBillFinishPageViewModel`** (Class) — `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillFinishPageViewModel.cs:11`
- **`CreateSupplierBillContractorPageViewModel`** (Class) — `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillContractorPageViewModel.cs:27`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `WizardPageViewModelBase` | Class | `src/client/Telemart.Client/ViewModels/Base/WizardPageViewModelBase.cs` | 14 |
| `CreateSupplierBillTextPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillTextPageViewModel.cs` | 30 |
| `CreateSupplierBillMatchingPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillMatchingPageViewModel.cs` | 20 |
| `CreateSupplierBillFinishPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillFinishPageViewModel.cs` | 11 |
| `CreateSupplierBillContractorPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillContractorPageViewModel.cs` | 27 |
| `CreateSupplierBillConfirmPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/SupplierBill/Create/CreateSupplierBillConfirmPageViewModel.cs` | 21 |
| `SelectEntitiesPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/CreateScanSheetForEntities/SelectEntitiesPageViewModel.cs` | 25 |
| `SearchCriteriaPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/CreateScanSheetForEntities/SearchCriteriaPageViewModel.cs` | 23 |
| `CompletePageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/CreateScanSheetForEntities/CompletePageViewModel.cs` | 7 |
| `SelectPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/CreateScanSheet/SelectPageViewModel.cs` | 30 |
| `SearchPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/CreateScanSheet/SearchPageViewModel.cs` | 26 |
| `FinishPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Store/CreateScanSheet/FinishPageViewModel.cs` | 7 |
| `ResolutionPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Diagnostics/ResolutionPageViewModel.cs` | 22 |
| `RejectPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Diagnostics/RejectPageViewModel.cs` | 16 |
| `DiagnoseFinishPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Diagnostics/DiagnoseFinishPageViewModel.cs` | 12 |
| `ConfirmRepairPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Diagnostics/ConfirmRepairPageViewModel.cs` | 22 |
| `ConfirmProductReturnPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Diagnostics/ConfirmProductReturnPageViewModel.cs` | 27 |
| `SrFinishPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/SrFinishPageViewModel.cs` | 11 |
| `SelectProductPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/SelectProductPageViewModel.cs` | 18 |
| `SelectOrderPageViewModel` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/SelectOrderPageViewModel.cs` | 30 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `HandleOkAsync → GetReturnMoneyString` | cross_community | 4 |
| `OnGoForward → ToUahStr` | cross_community | 4 |
| `OnGoForward → ToUsdStr` | cross_community | 4 |
| `OnGoForward → ToEurStr` | cross_community | 4 |
| `OnGoForward → GetEpsilon` | intra_community | 3 |
| `OnGoForward → GetById` | cross_community | 3 |
| `OnGoForward → ValidationResultViewModelParameter` | intra_community | 3 |
| `OnGoForward → GetStringParts` | intra_community | 3 |
| `OnGoForward → ServiceRequestCreateFromOrderViewMessage` | intra_community | 3 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Telemart.Client | 6 calls |
| Order | 3 calls |
| Diagnostics | 2 calls |
| Services | 2 calls |
| ServiceRequests | 2 calls |
| ProductInformation | 1 calls |
| Business | 1 calls |
| Refund | 1 calls |

## How to Explore

1. `gitnexus_context({name: "WizardPageViewModelBase"})` — see callers and callees
2. `gitnexus_query({query: "create"})` — find related execution flows
3. Read key files listed above for implementation details
