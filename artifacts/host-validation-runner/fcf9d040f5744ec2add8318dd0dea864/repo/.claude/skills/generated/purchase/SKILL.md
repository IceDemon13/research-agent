---
name: purchase
description: "Skill for the Purchase area of telemart_soft_test. 43 symbols across 14 files."
---

# Purchase

43 symbols | 14 files | Cohesion: 76%

## When to Use

- Working with code in `src/`
- Understanding how InvoiceCreateDto, SetSourceParameter, SetSourceViewItemBase work
- Modifying purchase-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Store/Purchase/SetInvoiceSourceViewModel.cs` | CreateInvoiceAsync, CreateInvoiceInternalAsync, CreateNewInvoiceInternal, OnInitializeInDesignMode, HandleLoadedAsync (+4) |
| `src/client/Telemart.Client/ViewModels/Store/Purchase/CreateReasonNoProductModel.cs` | HandleLoadedAsync, LoadSuppliersAsync, LoadOrderProductForReasonAsync, LoadProductInfoAsync, HandleOkAsync (+3) |
| `src/client/Telemart.Client/ViewModels/Store/Purchase/SetWarehouseSourceViewModel.cs` | OnInitializeInDesignMode, GetItems, HandleOkAsync, GetPurchaseSaveSource, TrySetSourceWithoutWhiteStockAsync (+1) |
| `src/client/Telemart.Client/ViewModels/Store/Purchase/SetMovementSourceViewModel.cs` | HandleLoadedAsync, OnInitializeInDesignMode, ToViewItem, GetItems, HandleOkAsync (+1) |
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | SetWarehouseSourceAsync, SetMovementAsync, CheckCompatibilityMultipleAsync |
| `src/client/Telemart.Client.TransferObjects/PurchaseSourceSaveDto.cs` | Warehouse, Movement, Purchase |
| `src/client/Telemart.Client.TransferObjects/InvoiceCreateDto.cs` | InvoiceCreateDto |
| `src/client/Telemart.Client/ViewModels/Store/Invoice/CreateInvoiceViewModel.cs` | HandleOkAsync |
| `src/client/Telemart.Client/ViewModels/Store/Purchase/SetSourceParameter.cs` | SetSourceParameter |
| `src/client/Telemart.Client/ViewModels/Store/Purchase/SetInvoiceSourceViewItem.cs` | Create |

## Entry Points

Start here when exploring this area:

- **`InvoiceCreateDto`** (Class) — `src/client/Telemart.Client.TransferObjects/InvoiceCreateDto.cs:5`
- **`SetSourceParameter`** (Class) — `src/client/Telemart.Client/ViewModels/Store/Purchase/SetSourceParameter.cs:6`
- **`SetSourceViewItemBase`** (Class) — `src/client/Telemart.Client/ViewModels/Store/Purchase/SetSourceViewItemBase.cs:6`
- **`Create`** (Method) — `src/client/Telemart.Client/ViewModels/Store/Purchase/SetInvoiceSourceViewItem.cs:111`
- **`Warehouse`** (Method) — `src/client/Telemart.Client.TransferObjects/PurchaseSourceSaveDto.cs:55`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `InvoiceCreateDto` | Class | `src/client/Telemart.Client.TransferObjects/InvoiceCreateDto.cs` | 5 |
| `SetSourceParameter` | Class | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetSourceParameter.cs` | 6 |
| `SetSourceViewItemBase` | Class | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetSourceViewItemBase.cs` | 6 |
| `Create` | Method | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetInvoiceSourceViewItem.cs` | 111 |
| `Warehouse` | Method | `src/client/Telemart.Client.TransferObjects/PurchaseSourceSaveDto.cs` | 55 |
| `Movement` | Method | `src/client/Telemart.Client.TransferObjects/PurchaseSourceSaveDto.cs` | 69 |
| `Purchase` | Method | `src/client/Telemart.Client.TransferObjects/PurchaseSourceSaveDto.cs` | 88 |
| `GetValidationResults` | Method | `src/client/Telemart.Client.Data/Requests/Features/Catalog/TransferObjects/CheckCompatibility/CheckCompatibilityResponse.cs` | 15 |
| `SetWarehouseSourceViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetWarehouseSourceViewItem.cs` | 2 |
| `SetMovementSourceViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetMovementSourceViewItem.cs` | 4 |
| `CreateInvoiceAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetInvoiceSourceViewModel.cs` | 262 |
| `CreateInvoiceInternalAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetInvoiceSourceViewModel.cs` | 274 |
| `CreateNewInvoiceInternal` | Method | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetInvoiceSourceViewModel.cs` | 320 |
| `HandleOkAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/CreateInvoiceViewModel.cs` | 244 |
| `OnInitializeInDesignMode` | Method | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetWarehouseSourceViewModel.cs` | 175 |
| `GetItems` | Method | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetWarehouseSourceViewModel.cs` | 192 |
| `SetWarehouseSourceAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | 5694 |
| `SetMovementAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | 5765 |
| `OnInitializeInDesignMode` | Method | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetInvoiceSourceViewModel.cs` | 156 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Purchase/SetInvoiceSourceViewModel.cs` | 178 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `HandleOkAsync → ValidationResultViewModelParameter` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Order | 7 calls |
| Business | 3 calls |
| ServiceRequests | 2 calls |
| Telemart.Client.Dictionaries | 1 calls |

## How to Explore

1. `gitnexus_context({name: "InvoiceCreateDto"})` — see callers and callees
2. `gitnexus_query({query: "purchase"})` — find related execution flows
3. Read key files listed above for implementation details
