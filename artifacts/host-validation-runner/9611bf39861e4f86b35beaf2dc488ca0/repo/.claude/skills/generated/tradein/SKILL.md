---
name: tradein
description: "Skill for the TradeIn area of telemart_soft_test. 95 symbols across 30 files."
---

# TradeIn

95 symbols | 30 files | Cohesion: 61%

## When to Use

- Working with code in `src/`
- Understanding how ComplaintsFilteringItem, TradeInCreateTrackNumberSourceViewItem, HandleErrorsAsync work
- Modifying tradein-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | CustomerSearchAsync, EvaluateAsync, ОverEvaluateRequestAsync, CancelRequestAsync, TestRequestAsync (+29) |
| `src/client/Telemart.Client/ViewModels/TradeIn/TradeInCreateViewModel.cs` | CustomerSearchAsync, SearchOrderAsync, ClearProperties, HandleLoadedAsync, LoadTradeInIndicatorValuesAsync (+5) |
| `src/client/Telemart.Client/ViewModels/TradeIn/TradeInsViewModel.cs` | OverReceiveAsync, HandleLoadedAsync, LoadWarehousesAsync, LoadTradeInIndicatorValuesAsync, LoadCategoriesAsync (+3) |
| `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserSearchTemplatesViewModel.cs` | DeleteAsync, AddFeatureAsync, DeleteFeatureAsync |
| `src/client/Telemart.Client/ViewModels/Parser/Dictionary/CompareProductByFeaturesViewModel.cs` | HandleOkAsync, SaveAsync, FindAsync |
| `src/client/Telemart.Client/Reports/TradeIn/TradeInReportPrinter.cs` | PrintActAsync, GetTradeInActInBytesAsync, GetHtmlAsync |
| `src/client/Telemart.Client/ViewModels/TradeIn/TradeInFilterViewModel.cs` | RefreshAsync, RefreshEmployeesAsync, RefreshTradeInIndicatorValuesAsync |
| `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEvaluateViewModel.cs` | HandleLoadedAsync, LoadOlxSearchUrlAsync, LoadOverclockersSearchUrlAsync |
| `src/client/Telemart.Client/ViewModels/Quotas/QuotasViewModel.cs` | DeleteQuotaAsync, EditQuotaAsync |
| `src/client/Telemart.Client/ViewModels/Parser/Dictionary/FeatureValueParserDictionaryViewModel.cs` | UpdateParserAliasesAsync, DeleteParserAliasesAsync |

## Entry Points

Start here when exploring this area:

- **`ComplaintsFilteringItem`** (Class) — `src/client/Telemart.Client/ViewModels/Complaint/ComplaintsFilteringItem.cs:7`
- **`TradeInCreateTrackNumberSourceViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/TradeIn/TradeInCreateTrackNumberSourceViewItem.cs:10`
- **`HandleErrorsAsync`** (Method) — `src/client/Telemart.Client/Common/ErrorHandler/ResultErrorHandler.cs:28`
- **`CutString`** (Method) — `src/client/Telemart.Client.Core/Extensions/StringExtensions.cs:133`
- **`PrintActAsync`** (Method) — `src/client/Telemart.Client/Reports/TradeIn/TradeInReportPrinter.cs:22`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `ComplaintsFilteringItem` | Class | `src/client/Telemart.Client/ViewModels/Complaint/ComplaintsFilteringItem.cs` | 7 |
| `TradeInCreateTrackNumberSourceViewItem` | Class | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInCreateTrackNumberSourceViewItem.cs` | 10 |
| `HandleErrorsAsync` | Method | `src/client/Telemart.Client/Common/ErrorHandler/ResultErrorHandler.cs` | 28 |
| `CutString` | Method | `src/client/Telemart.Client.Core/Extensions/StringExtensions.cs` | 133 |
| `PrintActAsync` | Method | `src/client/Telemart.Client/Reports/TradeIn/TradeInReportPrinter.cs` | 22 |
| `GetTradeInActInBytesAsync` | Method | `src/client/Telemart.Client/Reports/TradeIn/TradeInReportPrinter.cs` | 48 |
| `GetUrl` | Method | `src/client/Telemart.Client/Helpers/DocumentsBotHelper.cs` | 7 |
| `RefreshAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInFilterViewModel.cs` | 168 |
| `OverReceiveAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInsViewModel.cs` | 362 |
| `CustomerSearchAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | 747 |
| `EvaluateAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | 849 |
| `ОverEvaluateRequestAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | 974 |
| `CancelRequestAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | 1036 |
| `TestRequestAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | 1092 |
| `RemoveEDocumentAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | 1372 |
| `GetProductMaxPriceAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | 1625 |
| `CustomerSearchAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInCreateViewModel.cs` | 544 |
| `SearchOrderAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInCreateViewModel.cs` | 584 |
| `ClearProperties` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInCreateViewModel.cs` | 640 |
| `LoadCancelReasonsAsync` | Method | `src/client/Telemart.Client/ViewModels/TradeIn/TradeInCancelReasonViewModel.cs` | 67 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `CreateCallByOrderAsync → ValidationResultViewModelParameter` | cross_community | 5 |
| `HandleOkAsync → ValidationResultViewModelParameter` | cross_community | 5 |
| `QueryPricesByIdsAsync → ValidationResultViewModelParameter` | cross_community | 5 |
| `QuerySimplePricesByIdsAsync → ValidationResultViewModelParameter` | cross_community | 5 |
| `SavePricesAsync → ValidationResultViewModelParameter` | cross_community | 5 |
| `CalculateExtraChargeAsync → ValidationResultViewModelParameter` | cross_community | 5 |
| `CalculatePricesAsync → ValidationResultViewModelParameter` | cross_community | 5 |
| `SaveRatesAsync → ValidationResultViewModelParameter` | cross_community | 5 |
| `SentCheckAsync → ConfirmViewModelParameter` | cross_community | 4 |
| `CreateCallByOrderAsync → ConfirmViewModelParameter` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Order | 41 calls |
| ServiceRequests | 8 calls |
| Services | 5 calls |
| Telemart.Client | 2 calls |
| CreateScanSheetForEntities | 1 calls |
| ViewModels | 1 calls |
| Warehouse | 1 calls |
| Locations | 1 calls |

## How to Explore

1. `gitnexus_context({name: "ComplaintsFilteringItem"})` — see callers and callees
2. `gitnexus_query({query: "tradein"})` — find related execution flows
3. Read key files listed above for implementation details
