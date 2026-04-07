---
name: dictionary
description: "Skill for the Dictionary area of telemart_soft_test. 45 symbols across 19 files."
---

# Dictionary

45 symbols | 19 files | Cohesion: 73%

## When to Use

- Working with code in `src/`
- Understanding how ParserAliasViewItemWrapper, ParserAliasViewItem, ProductComparsionDto work
- Modifying dictionary-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserDictionaryViewModelBase.cs` | CompareAutoAsync, DeclOfNum, DeleteAsync, GetSelectedItemsToProcess, Export (+9) |
| `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ProductParserDictionaryViewModel.cs` | MapTransferObjectToViewItem, ExportToExcel, CompareAutoAsync, DeclOfNum, SearchProductsInCatalogAsync |
| `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserDictionaryFilter.cs` | RefreshValuesAsync, RefreshCategoriesAsync, RefreshContractorsAsync, RefreshParserAliasStatesAsync |
| `src/client/Telemart.Client/ViewModels/Parser/Dictionary/FeatureValueParserDictionaryViewModel.cs` | MapTransferObjectToViewItem, ExportToExcel, CompareAliasAuto |
| `src/client/Telemart.Client/ViewModels/Parser/Dictionary/FeatureParserDictionaryViewModel.cs` | MapTransferObjectToViewItem, ExportToExcel, CompareAliasAuto |
| `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeFilterViewModel.cs` | RefreshAsync, RefreshEmployeesAsync |
| `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/SelectProductPageViewModel.cs` | HandleLoadedAsync, RefreshGroupsAsync |
| `src/client/Telemart.Client.Data/WebClient/IWebClient.cs` | ExecuteApiRequestAsync |
| `src/client/Telemart.Client/WebClient/TelemartWebClient.cs` | ExecuteApiRequestAsync |
| `src/client/Telemart.Client/Validators/FiscalSettingsValidator.cs` | ValidateAsync |

## Entry Points

Start here when exploring this area:

- **`ParserAliasViewItemWrapper`** (Class) — `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserAliasViewItemWrapper.cs:8`
- **`ParserAliasViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserAliasViewItem.cs:9`
- **`ProductComparsionDto`** (Class) — `src/client/Telemart.Client.TransferObjects/ProductComparsionDto.cs:4`
- **`ExecuteApiRequestAsync`** (Method) — `src/client/Telemart.Client/WebClient/TelemartWebClient.cs:140`
- **`ValidateAsync`** (Method) — `src/client/Telemart.Client/Validators/FiscalSettingsValidator.cs:25`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `ParserAliasViewItemWrapper` | Class | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserAliasViewItemWrapper.cs` | 8 |
| `ParserAliasViewItem` | Class | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserAliasViewItem.cs` | 9 |
| `ProductComparsionDto` | Class | `src/client/Telemart.Client.TransferObjects/ProductComparsionDto.cs` | 4 |
| `IParserAliasViewItem` | Interface | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/IParserAliasViewItem.cs` | 6 |
| `ExecuteApiRequestAsync` | Method | `src/client/Telemart.Client/WebClient/TelemartWebClient.cs` | 140 |
| `ValidateAsync` | Method | `src/client/Telemart.Client/Validators/FiscalSettingsValidator.cs` | 25 |
| `RefreshAsync` | Method | `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeFilterViewModel.cs` | 79 |
| `BuildServiceRequestReportAsync` | Method | `src/client/Telemart.Client/Business/ServiceRequest/ServiceRequestReportBuilder.cs` | 11 |
| `PrintAsync` | Method | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/ServiceRequestPrinter.cs` | 29 |
| `RefreshValuesAsync` | Method | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserDictionaryFilter.cs` | 106 |
| `ExecuteApiRequestAsync` | Method | `src/client/Telemart.Client.Data/WebClient/IWebClient.cs` | 37 |
| `RefreshEmployeesAsync` | Method | `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeFilterViewModel.cs` | 88 |
| `FetchEmployeesAsync` | Method | `src/client/Telemart.Client/Business/Audit/ExternalPaymentAuditEntryProcessorBuilder.cs` | 69 |
| `RefreshCategoriesAsync` | Method | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserDictionaryFilter.cs` | 122 |
| `RefreshContractorsAsync` | Method | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserDictionaryFilter.cs` | 137 |
| `RefreshParserAliasStatesAsync` | Method | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ParserDictionaryFilter.cs` | 158 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/SelectProductPageViewModel.cs` | 89 |
| `RefreshGroupsAsync` | Method | `src/client/Telemart.Client/ViewModels/Service/ServiceRequests/Create/SelectProductPageViewModel.cs` | 97 |
| `MapTransferObjectToViewItem` | Method | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/ProductParserDictionaryViewModel.cs` | 201 |
| `MapTransferObjectToViewItem` | Method | `src/client/Telemart.Client/ViewModels/Parser/Dictionary/FeatureValueParserDictionaryViewModel.cs` | 169 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `PrintAsync → BuildRequest` | cross_community | 6 |
| `RefreshAsync → ShowMessageBoxError` | cross_community | 6 |
| `RefreshAsync → ExecuteAsync` | cross_community | 6 |
| `RefreshAsync → PagingInfo` | cross_community | 6 |
| `RefreshAsync → FetchPageAsync` | cross_community | 6 |
| `PrintAsync → ShowMessageBox` | cross_community | 5 |
| `PrintAsync → JsonSerializer` | cross_community | 5 |
| `PrintAsync → GetIntValue` | cross_community | 5 |
| `PrintAsync → SkipTakeUrlParameters` | cross_community | 5 |
| `PrintAsync → BuildRequest` | cross_community | 5 |

## Connected Areas

| Area | Connections |
|------|-------------|
| WebClient | 6 calls |
| ServiceRequests | 4 calls |
| Order | 2 calls |
| ProductImages | 2 calls |
| SalesMap | 1 calls |
| Create | 1 calls |
| Product | 1 calls |

## How to Explore

1. `gitnexus_context({name: "ParserAliasViewItemWrapper"})` — see callers and callees
2. `gitnexus_query({query: "dictionary"})` — find related execution flows
3. Read key files listed above for implementation details
