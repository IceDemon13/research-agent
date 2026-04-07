---
name: returninvoice
description: "Skill for the ReturnInvoice area of telemart_soft_test. 46 symbols across 25 files."
---

# ReturnInvoice

46 symbols | 25 files | Cohesion: 83%

## When to Use

- Working with code in `src/`
- Understanding how ServiceRequestDocumentDto, ReturnInvoiceDocumentDto, ManyReturnInvoiceProductViewItem work
- Modifying returninvoice-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/CreateReturnInvoiceViewModel.cs` | SelectNpWarehouse, HandleLoadedAsync, QueryCitiesAsync, QueryWarehousesAsync, GetSummaryItems (+1) |
| `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ManyReturnInvoicesViewModel.cs` | SelectNpWarehouse, HandleLoadedAsync, LoadContractorsAsync, LoadWarehousesAsync, LoadCitiesAsync |
| `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ChangeAddressReturnInvoiceViewModel.cs` | SelectNpWarehouse, HandleLoadedAsync, QueryWarehousesAsync, QueryCitiesAsync, QueryInvoiceAsync |
| `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ReturnInvoiceViewModel.cs` | PrintSnAsync, PrintReturnInvoiceAssemblyAsync, PrintReturnInvoiceSupplierAsync, GetPrintSettingsAsync |
| `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/CreateManyReturnInvoicesViewModel.cs` | HandleLoadedAsync, LoadWarehouseProductSourcesAsync, LoadAvailInvoiceProductSerialDtoAsync |
| `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/StoreReturnInvoicesFilterViewModel.cs` | RefreshAsync, RefreshWarehousesAsync, RefreshContractorsAsync |
| `src/client/Telemart.Client/ViewModels/Dialogs/AddDocuments/AddDocumentViewItem.cs` | GetExtension, GetDataAsync |
| `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | SelectDeliveryAddress |
| `src/client/Telemart.Client/ViewModels/TradeIn/TradeInCreateViewModel.cs` | SelectDeliveryAddress |
| `src/client/Telemart.Client/ViewModels/Novaposhta/UpdateNovaposhtaTtnViewModel.cs` | SelectNpWarehouse |

## Entry Points

Start here when exploring this area:

- **`ServiceRequestDocumentDto`** (Class) — `src/client/Telemart.Client.TransferObjects/ServiceRequestDocumentDto.cs:4`
- **`ReturnInvoiceDocumentDto`** (Class) — `src/client/Telemart.Client.TransferObjects/ReturnInvoice/ReturnInvoiceDocumentDto.cs:4`
- **`ManyReturnInvoiceProductViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ManyReturnInvoiceProductViewItem.cs:9`
- **`ReturnInvoiceProductCreateDto`** (Class) — `src/client/Telemart.Client.TransferObjects/ReturnInvoice/ReturnInvoiceProductCreateDto.cs:4`
- **`GetDeliveryServiceData`** (Method) — `src/client/Telemart.Client/ViewModels/Store/Order/SelectDeliveryWarehouseViewModel.cs:80`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `ServiceRequestDocumentDto` | Class | `src/client/Telemart.Client.TransferObjects/ServiceRequestDocumentDto.cs` | 4 |
| `ReturnInvoiceDocumentDto` | Class | `src/client/Telemart.Client.TransferObjects/ReturnInvoice/ReturnInvoiceDocumentDto.cs` | 4 |
| `ManyReturnInvoiceProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ManyReturnInvoiceProductViewItem.cs` | 9 |
| `ReturnInvoiceProductCreateDto` | Class | `src/client/Telemart.Client.TransferObjects/ReturnInvoice/ReturnInvoiceProductCreateDto.cs` | 4 |
| `GetDeliveryServiceData` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/SelectDeliveryWarehouseViewModel.cs` | 80 |
| `GetExtension` | Method | `src/client/Telemart.Client/ViewModels/Dialogs/AddDocuments/AddDocumentViewItem.cs` | 84 |
| `GetDataAsync` | Method | `src/client/Telemart.Client/ViewModels/Dialogs/AddDocuments/AddDocumentViewItem.cs` | 86 |
| `RefreshAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/StoreReturnInvoicesFilterViewModel.cs` | 188 |
| `SelectDeliveryAddress` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | 1741 |
| `SelectDeliveryAddress` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInCreateViewModel.cs` | 894 |
| `SelectNpWarehouse` | Method | `src/client/Telemart.Client/ViewModels/Novaposhta/UpdateNovaposhtaTtnViewModel.cs` | 239 |
| `SelectNpWarehouse` | Method | `src/client/Telemart.Client/ViewModels/Novaposhta/CreateNovaposhtaTtnViewModel.cs` | 533 |
| `SelectNpWarehouse` | Method | `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ManyReturnInvoicesViewModel.cs` | 290 |
| `SelectNpWarehouse` | Method | `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/CreateReturnInvoiceViewModel.cs` | 365 |
| `SelectNpWarehouse` | Method | `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ChangeAddressReturnInvoiceViewModel.cs` | 261 |
| `SelectDeliveryAddress` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | 4077 |
| `SelectDeliveryAddress` | Method | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/LogisticsPageViewModel.cs` | 278 |
| `SelectDeliveryAddress` | Method | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/CreateManyServiceRequestStep5ViewModel.cs` | 288 |
| `CreateDocumentAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInAddDocumentViewModel.cs` | 56 |
| `CreateDocumentAsync` | Method | `src/client/Telemart.Client/ViewModels/SupplierBill/SupplierBillAddDocumentViewModel.cs` | 35 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Order | 10 calls |
| TradeIn | 2 calls |
| ServiceRequests | 1 calls |
| ProductInformation | 1 calls |
| Create | 1 calls |
| ServiceProducts | 1 calls |

## How to Explore

1. `gitnexus_context({name: "ServiceRequestDocumentDto"})` — see callers and callees
2. `gitnexus_query({query: "returninvoice"})` — find related execution flows
3. Read key files listed above for implementation details
