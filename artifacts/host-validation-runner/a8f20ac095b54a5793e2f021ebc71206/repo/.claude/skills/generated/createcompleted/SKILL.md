---
name: createcompleted
description: "Skill for the CreateCompleted area of telemart_soft_test. 47 symbols across 8 files."
---

# CreateCompleted

47 symbols | 8 files | Cohesion: 57%

## When to Use

- Working with code in `src/`
- Understanding how ContractorTemplateDto, BonusConfirmationViewItem, Validate work
- Modifying createcompleted-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | HandleLoadedAsync, FetchContractorsAsync, FetchCitiesAsync, FetchWarehousesAsync, FetchWarehouseDeliveriesAsync (+35) |
| `src/client/Telemart.Client.TransferObjects/ContractorTemplateDto.cs` | ContractorTemplateDto |
| `src/client/Telemart.Client/ViewModels/Common/RequisitesViewItem.cs` | RaiseProperties |
| `src/client/Telemart.Client/Common/MvvmEnhancements/ObservableRangeValidatableCollection.cs` | Validate |
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | AddBonusAsync |
| `src/client/Telemart.Client/ViewModels/Store/Order/BonusConfirmationViewItem.cs` | BonusConfirmationViewItem |
| `src/client/Telemart.Client/Common/Utils/IdGenerator.cs` | GetNext |
| `src/client/Telemart.Client/Common/Utils/IIdGenerator.cs` | GetNext |

## Entry Points

Start here when exploring this area:

- **`ContractorTemplateDto`** (Class) — `src/client/Telemart.Client.TransferObjects/ContractorTemplateDto.cs:4`
- **`BonusConfirmationViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Store/Order/BonusConfirmationViewItem.cs:7`
- **`Validate`** (Method) — `src/client/Telemart.Client/Common/MvvmEnhancements/ObservableRangeValidatableCollection.cs:21`
- **`GetNext`** (Method) — `src/client/Telemart.Client/Common/Utils/IdGenerator.cs:6`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `ContractorTemplateDto` | Class | `src/client/Telemart.Client.TransferObjects/ContractorTemplateDto.cs` | 4 |
| `BonusConfirmationViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/BonusConfirmationViewItem.cs` | 7 |
| `Validate` | Method | `src/client/Telemart.Client/Common/MvvmEnhancements/ObservableRangeValidatableCollection.cs` | 21 |
| `GetNext` | Method | `src/client/Telemart.Client/Common/Utils/IdGenerator.cs` | 6 |
| `RaiseProperties` | Method | `src/client/Telemart.Client/ViewModels/Common/RequisitesViewItem.cs` | 116 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 315 |
| `FetchContractorsAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1081 |
| `FetchCitiesAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1090 |
| `FetchWarehousesAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1099 |
| `FetchWarehouseDeliveriesAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1131 |
| `FetchShopsAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1142 |
| `FetchEmployeesAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1147 |
| `AddBonusAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | 9539 |
| `EditBonus` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1185 |
| `AddBonus` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1233 |
| `ProcessBonuses` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1621 |
| `AppliedBonusesChanged` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1638 |
| `DeleteProduct` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 696 |
| `RemoveBonus` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1158 |
| `RefreshPaymentInfo` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderViewModel.cs` | 1349 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `AddBonusAsync → CreateLockRequest` | cross_community | 4 |
| `AddBonusAsync → SendMessage` | cross_community | 4 |
| `AddBonusAsync → CreateMessage` | cross_community | 4 |
| `AddBonusAsync → CreateUnlockRequest` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Order | 19 calls |
| TradeIn | 4 calls |
| ServiceRequests | 3 calls |
| Telemart.Client.Tests | 2 calls |
| Telemart.Client.Dictionaries | 2 calls |
| Telemart.Client.TransferObjects | 2 calls |
| ServiceRepairs | 1 calls |
| Services | 1 calls |

## How to Explore

1. `gitnexus_context({name: "ContractorTemplateDto"})` — see callers and callees
2. `gitnexus_query({query: "createcompleted"})` — find related execution flows
3. Read key files listed above for implementation details
