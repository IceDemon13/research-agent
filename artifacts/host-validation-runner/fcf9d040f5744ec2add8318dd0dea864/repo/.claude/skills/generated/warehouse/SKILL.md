---
name: warehouse
description: "Skill for the Warehouse area of telemart_soft_test. 80 symbols across 46 files."
---

# Warehouse

80 symbols | 46 files | Cohesion: 65%

## When to Use

- Working with code in `src/`
- Understanding how WarehouseDeliveryViewItem, SupplierBillViewItem, PromoCodeViewItem work
- Modifying warehouse-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Warehouse/WarehouseViewModel.cs` | MapWarehouseDelivery, MapWarehousePerfomance, HandleLoadedAsync, RefreshCitiesAsync, RefreshEmployeesAsync (+20) |
| `src/client/Telemart.Client/ViewModels/Warehouse/WarehousesViewModel.cs` | RefreshAsync, RefreshEmployeesAsync, RefreshCitiesAsync, OnWarehouseMessage, Map |
| `src/client/Telemart.Client/ViewModels/Warehouse/WarehousePerfomanceEditViewModel.cs` | HandleLoadedAsync, CreateEntityAsync, UpdateEntityAsync, MapViewItem |
| `src/client/Telemart.Client/ViewModels/Warehouse/WarehouseDeliveryEditViewModel.cs` | HandleLoadedAsync, GetNPCouirerCallIntervalsAsync |
| `src/client/Telemart.Client/ViewModels/Warehouse/WarehouseRouteCreateViewModel.cs` | HandleOkAsync, MapToDto |
| `src/client/Telemart.Client/ViewModels/Warehouse/WarehouseCopyViewModel.cs` | HandleLoadedAsync, LoadWarehouseAsync |
| `src/client/Telemart.Client/ViewModels/TradeInSegment/TradeInSegmentViewModel.cs` | HandleLoadedAsync |
| `src/client/Telemart.Client/ViewModels/Segment/SegmentViewModel.cs` | HandleLoadedAsync |
| `src/client/Telemart.Client/ViewModels/Reporting/ReportEditorViewModel.cs` | HandleLoadedAsync |
| `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeViewModel.cs` | HandleLoadedAsync |

## Entry Points

Start here when exploring this area:

- **`WarehouseDeliveryViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Warehouse/WarehouseDeliveryViewItem.cs:12`
- **`SupplierBillViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/SupplierBill/SupplierBillViewItem.cs:14`
- **`PromoCodeViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeViewItem.cs:7`
- **`PromoCodeFullViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeFullViewItem.cs:11`
- **`ServiceMovementViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Service/ServiceMovements/ServiceMovementViewItem.cs:13`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `WarehouseDeliveryViewItem` | Class | `src/client/Telemart.Client/ViewModels/Warehouse/WarehouseDeliveryViewItem.cs` | 12 |
| `SupplierBillViewItem` | Class | `src/client/Telemart.Client/ViewModels/SupplierBill/SupplierBillViewItem.cs` | 14 |
| `PromoCodeViewItem` | Class | `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeViewItem.cs` | 7 |
| `PromoCodeFullViewItem` | Class | `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeFullViewItem.cs` | 11 |
| `ServiceMovementViewItem` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceMovements/ServiceMovementViewItem.cs` | 13 |
| `ServiceCenterViewItem` | Class | `src/client/Telemart.Client/ViewModels/Service/ServiceCenters/ServiceCenterViewItem.cs` | 16 |
| `ReportViewItem` | Class | `src/client/Telemart.Client/ViewModels/Reporting/ViewItems/ReportViewItem.cs` | 12 |
| `RefundViewItem` | Class | `src/client/Telemart.Client/ViewModels/Money/Refund/RefundViewItem.cs` | 12 |
| `OrganizationViewItem` | Class | `src/client/Telemart.Client/ViewModels/Directories/Organization/OrganizationViewItem.cs` | 11 |
| `EmployeeRichViewItem` | Class | `src/client/Telemart.Client/ViewModels/Directories/Employee/EmployeeRichViewItem.cs` | 12 |
| `FeatureViewItem` | Class | `src/client/Telemart.Client/ViewModels/Content/ProductFeatureGroups/FeatureViewItem.cs` | 14 |
| `WarehousePerfomanceViewItem` | Class | `src/client/Telemart.Client/ViewModels/Warehouse/WarehousePerfomanceViewItem.cs` | 8 |
| `TradeInViewItem` | Class | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInViewItem.cs` | 14 |
| `PaymentViewItem` | Class | `src/client/Telemart.Client/ViewModels/Payments/PaymentViewItem.cs` | 7 |
| `TelemartEditorViewItemBase` | Class | `src/client/Telemart.Client/ViewModels/Base/TelemartEditorViewItemBase.cs` | 5 |
| `AssemblyTestGroupViewItem` | Class | `src/client/Telemart.Client/ViewModels/AssemblyTest/AssemblyTestGroupViewItem.cs` | 9 |
| `AssemblyTestGroupSimpleViewItem` | Class | `src/client/Telemart.Client/ViewModels/AssemblyTest/AssemblyTestGroupSimpleViewItem.cs` | 6 |
| `AdditionalServiceGroupSimpleViewItem` | Class | `src/client/Telemart.Client/ViewModels/AdditionalService/AdditionalServiceGroupSimpleViewItem.cs` | 7 |
| `ReturnInvoiceProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ReturnInvoiceProductViewItem.cs` | 10 |
| `SupplierWarehouseViewItem` | Class | `src/client/Telemart.Client/ViewModels/Directories/Contractor/SupplierWarehouseViewItem.cs` | 8 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `HandleLoadedAsync → SetCopyTitle` | cross_community | 4 |
| `HandleLoadedAsync → BeforeSetData` | cross_community | 4 |
| `HandleLoadedAsync → Update` | cross_community | 4 |
| `HandleLoadedAsync → AfterSetData` | cross_community | 4 |
| `HandleLoadedAsync → SetCopyTitle` | cross_community | 4 |
| `HandleLoadedAsync → BeforeSetData` | cross_community | 4 |
| `HandleLoadedAsync → Update` | cross_community | 4 |
| `HandleLoadedAsync → AfterSetData` | cross_community | 4 |
| `HandleLoadedAsync → SetCopyTitle` | cross_community | 4 |
| `HandleLoadedAsync → BeforeSetData` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Order | 8 calls |
| ServiceRequests | 2 calls |
| Base | 2 calls |
| TradeIn | 1 calls |

## How to Explore

1. `gitnexus_context({name: "WarehouseDeliveryViewItem"})` — see callers and callees
2. `gitnexus_query({query: "warehouse"})` — find related execution flows
3. Read key files listed above for implementation details
