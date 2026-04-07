---
name: invoice
description: "Skill for the Invoice area of telemart_soft_test. 43 symbols across 6 files."
---

# Invoice

43 symbols | 6 files | Cohesion: 64%

## When to Use

- Working with code in `src/`
- Understanding how EmployeeSimpleDto, InvoiceProductViewItem, InvoiceCategoryBudgetViewItem work
- Modifying invoice-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | HandleOkAsync, HandleLoadedAsync, GetQueryProductsCatalogRequest, GetSaveDto, AnalyzeAsync (+27) |
| `src/client/Telemart.Client/ViewModels/Store/Invoice/CreateInvoiceViewModel.cs` | ClearProperties, DateFromChangedCallbackAsync, RefreshSupplerWarehousesAsync, SelectedSupplierChangedCallbackAsync, SelectSupplierInternalAsync |
| `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceEditCategoryBudgetsViewModel.cs` | HandleOkAsync, OnSelectedBudgetChanged, AddCurrentCategoryBudgetsToSaveCollection |
| `src/client/Telemart.Client.TransferObjects/EmployeeSimpleDto.cs` | EmployeeSimpleDto |
| `src/client/Telemart.Client/ViewModels/Store/InvoiceProductViewItem.cs` | InvoiceProductViewItem |
| `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceCategoryBudgetViewItem.cs` | InvoiceCategoryBudgetViewItem |

## Entry Points

Start here when exploring this area:

- **`EmployeeSimpleDto`** (Class) — `src/client/Telemart.Client.TransferObjects/EmployeeSimpleDto.cs:4`
- **`InvoiceProductViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Store/InvoiceProductViewItem.cs:13`
- **`InvoiceCategoryBudgetViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceCategoryBudgetViewItem.cs:6`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `EmployeeSimpleDto` | Class | `src/client/Telemart.Client.TransferObjects/EmployeeSimpleDto.cs` | 4 |
| `InvoiceProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/InvoiceProductViewItem.cs` | 13 |
| `InvoiceCategoryBudgetViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceCategoryBudgetViewItem.cs` | 6 |
| `HandleOkAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 381 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 425 |
| `GetQueryProductsCatalogRequest` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 488 |
| `GetSaveDto` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 493 |
| `AnalyzeAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 848 |
| `RefreshInvoiceAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 1673 |
| `ApplyEditingInfo` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 1812 |
| `GetPropertyNamesDependentOnState` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 1957 |
| `GetErrorMessage` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 478 |
| `OpenInvoiceAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 723 |
| `CloseInvoiceAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 770 |
| `PurchaseInvoiceAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 821 |
| `ArriveInvoiceAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 1010 |
| `DontReceiveInvoiceAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 1139 |
| `RefreshView` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 1377 |
| `Delay` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 1565 |
| `AddProductAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | 521 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `ReceiveInvoiceAsync → SetIsEditableForCurrentUser` | cross_community | 4 |
| `ReceiveInvoiceAsync → Sort` | cross_community | 4 |
| `ReceiveInvoiceAsync → GetSummaryItems` | cross_community | 4 |
| `ReceiveInvoiceAsync → SummaryViewItem` | cross_community | 4 |
| `ReceiveInvoiceAsync → Prices` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| ServiceRequests | 9 calls |
| Order | 6 calls |
| ProductInformation | 2 calls |
| Create | 1 calls |
| Store | 1 calls |
| TradeIn | 1 calls |

## How to Explore

1. `gitnexus_context({name: "EmployeeSimpleDto"})` — see callers and callees
2. `gitnexus_query({query: "invoice"})` — find related execution flows
3. Read key files listed above for implementation details
