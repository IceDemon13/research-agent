---
name: showcase
description: "Skill for the Showcase area of telemart_soft_test. 84 symbols across 24 files."
---

# Showcase

84 symbols | 24 files | Cohesion: 76%

## When to Use

- Working with code in `src/`
- Understanding how PickupProductViewItem, ShowcaseClusterViewItem, CurrencyTypeRateViewItem work
- Modifying showcase-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Showcase/AutoShowcasesViewModel.cs` | HandleLoadedAsync, RefreshLocationsAsync, RefreshWarehousesAsync, CalculateSummaryCapacityByWarehouses, AddAsync (+14) |
| `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseCategoryViewModel.cs` | HandleLoadedAsync, RefreshClusterCategoryShowcaseAllowSetQuantityAsync, RefreshShowcaseCategoriesAsync, RefreshClustersAsync, RefreshLocationsAsync (+8) |
| `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseClusterViewModel.cs` | AddShowcaseClusterAsync, HandleLoadedAsync, RefreshLocationsAsync, RefreshCategoriesAsync, RefreshClustersAsync (+5) |
| `src/client/Telemart.Client/ViewModels/Showcase/AutoShowcasesHistoryViewModel.cs` | HandleLoadedAsync, RefreshWarehousesAsync, RefreshCategoriesAsync, RefreshProductAsync, RefreshEmployeesAsync (+2) |
| `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseCategoryHistoryViewModel.cs` | HandleLoadedAsync, RefreshWarehousesAsync, RefreshCategoriesAsync, RefreshEmployeesAsync, LoadHistoriesByProductAsync |
| `src/client/Telemart.Client/ViewModels/Showcase/AutoShowcaseFilterViewModel.cs` | RefreshAsync, RefreshWarehousesAsync, RefreshClustersAsync, RefreshEmployeesAsync, RefreshCategoriesAsync |
| `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseFilterViewModel.cs` | RefreshAsync, RefreshWarehousesAsync, RefreshEmployeesAsync, RefreshCategoriesAsync |
| `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseViewModel.cs` | HandleLoadedAsync, SelectProductAsync, RefreshQuantityAsync |
| `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseClusterCreateViewModel.cs` | HandleLoadedAsync, RefreshCategoriesAsync, RefreshClustersAsync |
| `src/client/Telemart.Client/ViewModels/Tasks/PickupProductViewItem.cs` | PickupProductViewItem |

## Entry Points

Start here when exploring this area:

- **`PickupProductViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Tasks/PickupProductViewItem.cs:8`
- **`ShowcaseClusterViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseClusterViewItem.cs:8`
- **`CurrencyTypeRateViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Common/CurrencyTypeRateViewItem.cs:5`
- **`TelemartCloneableViewItemBase`** (Class) — `src/client/Telemart.Client/ViewModels/Base/TelemartCloneableViewItemBase.cs:5`
- **`AssembledComputerRuleReserveProductViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/AssembledComputerRule/AssembledComputerRuleReserveProductViewItem.cs:7`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `PickupProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/Tasks/PickupProductViewItem.cs` | 8 |
| `ShowcaseClusterViewItem` | Class | `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseClusterViewItem.cs` | 8 |
| `CurrencyTypeRateViewItem` | Class | `src/client/Telemart.Client/ViewModels/Common/CurrencyTypeRateViewItem.cs` | 5 |
| `TelemartCloneableViewItemBase` | Class | `src/client/Telemart.Client/ViewModels/Base/TelemartCloneableViewItemBase.cs` | 5 |
| `AssembledComputerRuleReserveProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/AssembledComputerRule/AssembledComputerRuleReserveProductViewItem.cs` | 7 |
| `InvoiceCurrencyRateViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceCurrencyRateViewItem.cs` | 5 |
| `ParserSearchTemplateViewItem` | Class | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserSearchTemplateViewItem.cs` | 11 |
| `ContractorCurrencyPermissionViewItem` | Class | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorCurrencyPermissionViewItem.cs` | 5 |
| `CreateCompletedOrderProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Order/CreateCompleted/CreateCompletedOrderProductViewItem.cs` | 16 |
| `AutoShowcasesHistoryViewItem` | Class | `src/client/Telemart.Client/ViewModels/Showcase/AutoShowcasesHistoryViewItem.cs` | 8 |
| `RefreshAsync` | Method | `src/client/Telemart.Client/ViewModels/Showcase/AutoShowcaseFilterViewModel.cs` | 101 |
| `RefreshAsync` | Method | `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseFilterViewModel.cs` | 99 |
| `AddShowcaseClusterAsync` | Method | `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseClusterViewModel.cs` | 272 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/Common/UpdateCurrencyRatesViewModel.cs` | 72 |
| `AddProductAsync` | Method | `src/client/Telemart.Client/ViewModels/AssembledComputerRule/AssembledComputerRuleReserveViewModel.cs` | 258 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/CompareProductByFeaturesViewModel.cs` | 101 |
| `AddCurrencyPermission` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | 1314 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseCategoryViewModel.cs` | 131 |
| `RefreshClusterCategoryShowcaseAllowSetQuantityAsync` | Method | `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseCategoryViewModel.cs` | 157 |
| `RefreshShowcaseCategoriesAsync` | Method | `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseCategoryViewModel.cs` | 166 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `HandleLoadedAsync → MovementFilteringItem` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Order | 10 calls |
| Create | 3 calls |
| TradeIn | 3 calls |
| ServiceRequests | 2 calls |
| Warehouse | 1 calls |
| Telemart.Client.PosTerminal.Ingenico | 1 calls |
| RobotProperties | 1 calls |
| Movement | 1 calls |

## How to Explore

1. `gitnexus_context({name: "PickupProductViewItem"})` — see callers and callees
2. `gitnexus_query({query: "showcase"})` — find related execution flows
3. Read key files listed above for implementation details
