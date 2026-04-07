---
name: contractor
description: "Skill for the Contractor area of telemart_soft_test. 45 symbols across 10 files."
---

# Contractor

45 symbols | 10 files | Cohesion: 72%

## When to Use

- Working with code in `src/`
- Understanding how ContractorViewItem, ContractorCityItem, SupplierCategoryAbcSaveDto work
- Modifying contractor-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | HandleLoadedAsync, RefreshContractorsAsync, RefreshCountriesAsync, RefreshEmployeesAsync, RefreshPositionsAsync (+13) |
| `src/client/Telemart.Client/ViewModels/Directories/Contractor/CreateContractorViewModel.cs` | HandleLoadedAsync, RefreshContractorsAsync, RefreshCountriesAsync, RefreshEmployeesAsync, RefreshWarehousesAsync (+2) |
| `src/client/Telemart.Client/ViewModels/Directories/Contractor/DirectoryContractorsViewModel.cs` | OnContractorMessage, RefreshContractorsAsync, Map, RefreshAsync, RefreshEmployeesAsync (+1) |
| `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorContactViewModel.cs` | HandleLoadedAsync, RefreshCountriesAsync, RefreshPositionsAsync, RefreshCitiesAsync, SelectedCountryChanged (+1) |
| `src/client/Telemart.Client/ViewModels/Directories/Contractor/SupplierCarryViewModel.cs` | HandleLoadedAsync, RefreshCitiesAsync, RefreshWarehousesAsync |
| `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewItem.cs` | ContractorViewItem |
| `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorCityItem.cs` | ContractorCityItem |
| `src/client/Telemart.Client.TransferObjects/SupplierCategoryAbcSaveDto.cs` | SupplierCategoryAbcSaveDto |
| `src/client/Telemart.Client/ViewModels/Directories/Contractor/SupplierCarryViewItem.cs` | SupplierCarryViewItem |
| `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorContactViewItem.cs` | Create |

## Entry Points

Start here when exploring this area:

- **`ContractorViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewItem.cs:18`
- **`ContractorCityItem`** (Class) — `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorCityItem.cs:2`
- **`SupplierCategoryAbcSaveDto`** (Class) — `src/client/Telemart.Client.TransferObjects/SupplierCategoryAbcSaveDto.cs:4`
- **`SupplierCarryViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Directories/Contractor/SupplierCarryViewItem.cs:10`
- **`Create`** (Method) — `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorContactViewItem.cs:74`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `ContractorViewItem` | Class | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewItem.cs` | 18 |
| `ContractorCityItem` | Class | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorCityItem.cs` | 2 |
| `SupplierCategoryAbcSaveDto` | Class | `src/client/Telemart.Client.TransferObjects/SupplierCategoryAbcSaveDto.cs` | 4 |
| `SupplierCarryViewItem` | Class | `src/client/Telemart.Client/ViewModels/Directories/Contractor/SupplierCarryViewItem.cs` | 10 |
| `Create` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorContactViewItem.cs` | 74 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | 567 |
| `RefreshContractorsAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | 1013 |
| `RefreshCountriesAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | 1060 |
| `RefreshEmployeesAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | 1076 |
| `RefreshPositionsAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | 1087 |
| `RefreshWarehousesAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | 1093 |
| `RefreshOwnershipFormsAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | 1101 |
| `RefreshSupplierInvoiceProcessorsAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | 1114 |
| `RefreshSupplierWarehousesAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/ContractorViewModel.cs` | 1191 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/CreateContractorViewModel.cs` | 199 |
| `RefreshContractorsAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/CreateContractorViewModel.cs` | 297 |
| `RefreshCountriesAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/CreateContractorViewModel.cs` | 336 |
| `RefreshEmployeesAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/CreateContractorViewModel.cs` | 352 |
| `RefreshWarehousesAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/CreateContractorViewModel.cs` | 363 |
| `OnContractorMessage` | Method | `src/client/Telemart.Client/ViewModels/Directories/Contractor/DirectoryContractorsViewModel.cs` | 380 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Order | 8 calls |
| Telemart.Client.Dictionaries | 3 calls |
| Warehouse | 1 calls |
| Locations | 1 calls |

## How to Explore

1. `gitnexus_context({name: "ContractorViewItem"})` — see callers and callees
2. `gitnexus_query({query: "contractor"})` — find related execution flows
3. Read key files listed above for implementation details
