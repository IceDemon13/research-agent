---
name: productsprices
description: "Skill for the ProductsPrices area of telemart_soft_test. 52 symbols across 9 files."
---

# ProductsPrices

52 symbols | 9 files | Cohesion: 72%

## When to Use

- Working with code in `src/`
- Understanding how ProductPriceViewItem, GetFullScript, CreateAsync work
- Modifying productsprices-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPricesViewModel.cs` | OldCalculatePricesAsync, DebugAsync, Message, OldCalculatePricesInternalAsync, QueryProductPricesAsync (+17) |
| `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | GetPriceValue, GetPricePrevValue, GetTagColor, GetPartialPay, GetPartialPayPb (+9) |
| `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceViewItem.cs` | SetCalculatePriceResult, GetEditablePriceDatas, ProductPriceViewItem, GetContext, GetErrors (+3) |
| `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPricesRobotViewModel.cs` | RefreshAsync, UseScriptFromCategoryChangedCallbackAsync |
| `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ExcelPricesImportEngine.cs` | MapRow, CalculateParserCategoryId |
| `src/client/Telemart.Client/Helpers/RobotHelper.cs` | GetFullScript |
| `src/client/Telemart.Client/ViewModels/Accessory/AccessoriesViewModel.cs` | Copy |
| `src/client/Telemart.Client/Business/PriceConversion/PriceConverterFactory.cs` | CreateAsync |
| `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceViewModel.cs` | SetCurrencyRateAsync |

## Entry Points

Start here when exploring this area:

- **`ProductPriceViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceViewItem.cs:21`
- **`GetFullScript`** (Method) — `src/client/Telemart.Client/Helpers/RobotHelper.cs:4`
- **`CreateAsync`** (Method) — `src/client/Telemart.Client/Business/PriceConversion/PriceConverterFactory.cs:23`
- **`OldCalculatePricesAsync`** (Method) — `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPricesViewModel.cs:486`
- **`DebugAsync`** (Method) — `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPricesViewModel.cs:559`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `ProductPriceViewItem` | Class | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceViewItem.cs` | 21 |
| `GetFullScript` | Method | `src/client/Telemart.Client/Helpers/RobotHelper.cs` | 4 |
| `CreateAsync` | Method | `src/client/Telemart.Client/Business/PriceConversion/PriceConverterFactory.cs` | 23 |
| `OldCalculatePricesAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPricesViewModel.cs` | 486 |
| `DebugAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPricesViewModel.cs` | 559 |
| `SetCalculatePriceResult` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceViewItem.cs` | 1109 |
| `GetEditablePriceDatas` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceViewItem.cs` | 901 |
| `GetPriceValue` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | 382 |
| `GetPricePrevValue` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | 387 |
| `GetTagColor` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | 392 |
| `GetPartialPay` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | 397 |
| `GetPartialPayPb` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | 402 |
| `GetPartialPayPumb` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | 407 |
| `GetPartialPayAb` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | 412 |
| `GetContext` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceViewItem.cs` | 985 |
| `GetPriceOldUsdValue` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | 417 |
| `GetPricePrevOldUsdValue` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | 422 |
| `GetPriceUsdValue` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPriceDataViewItem.cs` | 432 |
| `GetRobotScriptByCategoryAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPricesViewModel.cs` | 596 |
| `RefreshAsync` | Method | `src/client/Telemart.Client/ViewModels/Directories/ProductsPrices/ProductPricesRobotViewModel.cs` | 119 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `SendMovementAsync → QueryCurrencyTypeRates` | cross_community | 5 |
| `SendReturnInvoiceAsync → QueryCurrencyTypeRates` | cross_community | 4 |
| `CalculateByServiceMovementAsync → QueryCurrencyTypeRates` | cross_community | 4 |
| `SaveAsync → GetEditablePriceDatas` | intra_community | 3 |

## Connected Areas

| Area | Connections |
|------|-------------|
| SalesMap | 2 calls |
| ServiceRequests | 2 calls |
| Movement | 2 calls |
| PriceConversion | 1 calls |
| ProductImages | 1 calls |
| Order | 1 calls |

## How to Explore

1. `gitnexus_context({name: "ProductPriceViewItem"})` — see callers and callees
2. `gitnexus_query({query: "productsprices"})` — find related execution flows
3. Read key files listed above for implementation details
