---
name: movement
description: "Skill for the Movement area of telemart_soft_test. 87 symbols across 26 files."
---

# Movement

87 symbols | 26 files | Cohesion: 71%

## When to Use

- Working with code in `src/`
- Understanding how MovementProductSnViewItem, MovementViewItem, MovementFilteringItem work
- Modifying movement-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementViewModel.cs` | LoadValues, RecognizeBarcodeViewModelOnFinished, ProcessScanForAssembledComputers, SplitAdditionalServices, BeforeSetData (+24) |
| `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementsMassScanViewModel.cs` | OnSelectedProductChanged, RecognizeBarcodeViewModelOnFinished, ProcessScanForAssembledComputers, SplitAdditionalServices, HandleLoadedAsync (+10) |
| `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs` | Scan, ScanAssemblyProduct, IsScanned, RaiseProperties, SplitConsumableAdditionalServiceProduct (+4) |
| `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementsViewModel.cs` | HandleLoadedAsync, RefreshAsync, RefreshEmployeesAsync, RefreshWarehousesAsync, AddAsync (+3) |
| `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementDelayViewModel.cs` | ProductChanged, HandleLoadedAsync, LoadOrderProductsAsync |
| `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductSnViewItem.cs` | MovementProductSnViewItem, Scan |
| `src/client/Telemart.Client/ViewModels/Warehouse/Movement/CreateMovementViewModel.cs` | HandleLoadedAsync, CalculatePurposesEnabled |
| `src/client/Telemart.Client/ViewModels/Tasks/ProcessPickupProductsViewModel.cs` | SelectedProductChanged |
| `src/client/Telemart.Client/ViewModels/Store/StorePurchasesViewModel.cs` | CurrentPurchaseChangedCallback |
| `src/client/Telemart.Client/ViewModels/Showcase/ShowcasesViewModel.cs` | SelectedShowcaseChanged |

## Entry Points

Start here when exploring this area:

- **`MovementProductSnViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductSnViewItem.cs:5`
- **`MovementViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementViewItem.cs:12`
- **`MovementFilteringItem`** (Class) — `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementFilteringItem.cs:7`
- **`ClearProduct`** (Method) — `src/client/Telemart.Client/ViewModels/Store/Order/ProductInformation/ProductInformationViewModel.cs:350`
- **`Scan`** (Method) — `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs:467`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `MovementProductSnViewItem` | Class | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductSnViewItem.cs` | 5 |
| `MovementViewItem` | Class | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementViewItem.cs` | 12 |
| `MovementFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementFilteringItem.cs` | 7 |
| `ClearProduct` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/ProductInformation/ProductInformationViewModel.cs` | 350 |
| `Scan` | Method | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs` | 467 |
| `ScanAssemblyProduct` | Method | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs` | 479 |
| `IsScanned` | Method | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs` | 491 |
| `RaiseProperties` | Method | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs` | 542 |
| `Scan` | Method | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductSnViewItem.cs` | 43 |
| `Clone` | Method | `src/client/Telemart.Client.Core/Cloning/ReflectionObjectCloner.cs` | 7 |
| `SplitConsumableAdditionalServiceProduct` | Method | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs` | 315 |
| `SplitAdditionalService` | Method | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs` | 360 |
| `Clone` | Method | `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ReturnInvoiceViewItem.cs` | 118 |
| `SplitAssembledComputer` | Method | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs` | 387 |
| `SplitAssemblyService` | Method | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs` | 287 |
| `RefreshSerialNumbersAfterEditing` | Method | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementProductViewItem.cs` | 506 |
| `SelectedProductChanged` | Method | `src/client/Telemart.Client/ViewModels/Tasks/ProcessPickupProductsViewModel.cs` | 146 |
| `CurrentPurchaseChangedCallback` | Method | `src/client/Telemart.Client/ViewModels/Store/StorePurchasesViewModel.cs` | 369 |
| `SelectedShowcaseChanged` | Method | `src/client/Telemart.Client/ViewModels/Showcase/ShowcasesViewModel.cs` | 303 |
| `CurrentProductChangedCallback` | Method | `src/client/Telemart.Client/ViewModels/Showcase/AutoShowcasesViewModel.cs` | 835 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `SendMovementAsync → QueryCurrencyTypeRates` | cross_community | 5 |
| `DiagnoseRequestAsync → PropertyGridRowPropertyDescriptor` | cross_community | 4 |
| `SendMovementAsync → PackagePropertiesParameter` | cross_community | 4 |
| `SendMovementAsync → GetTotalWeightFact` | cross_community | 4 |
| `HandleLoadedAsync → MovementFilteringItem` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| ServiceRequests | 7 calls |
| Order | 5 calls |
| ProductsPrices | 3 calls |
| Store | 2 calls |
| Diagnostics | 2 calls |
| Telemart.Client.Dictionaries | 1 calls |
| Showcase | 1 calls |
| Warehouse | 1 calls |

## How to Explore

1. `gitnexus_context({name: "MovementProductSnViewItem"})` — see callers and callees
2. `gitnexus_query({query: "movement"})` — find related execution flows
3. Read key files listed above for implementation details
