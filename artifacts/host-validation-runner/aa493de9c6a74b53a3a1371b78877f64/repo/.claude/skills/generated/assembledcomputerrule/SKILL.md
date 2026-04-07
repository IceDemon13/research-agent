---
name: assembledcomputerrule
description: "Skill for the AssembledComputerRule area of telemart_soft_test. 53 symbols across 37 files."
---

# AssembledComputerRule

53 symbols | 37 files | Cohesion: 71%

## When to Use

- Working with code in `src/`
- Understanding how TradeInSegmentCategorySettingsFeatureViewItem, TraderInCoefViewItem, ShowcaseCategoryHistoryViewItem work
- Modifying assembledcomputerrule-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/AssembledComputerRule/AssembledComputerRuleViewModel.cs` | AddCategory, CheckAsync, RefreshProductInfo, AddProductAsync, RefreshProducts (+1) |
| `src/client/Telemart.Client/ViewModels/AssembledComputerRule/AssembledComputerRuleIgnoreViewModel.cs` | HandleLoadedAsync, LoadCategoriesAsync, LoadFeaturesAsync, LoadSlotHostsAsync, MapAssemblySlotHostConsumerItem (+1) |
| `src/client/Telemart.Client/ViewModels/AssembledComputerRule/AssembledComputerRuleViewItem.cs` | BuildMetadata, IsPartNumberValid, BuildName, BuildModel, PartNumberChanged |
| `src/client/Telemart.Client/ViewModels/AssembledComputerRule/AssembledComputerRuleCreateCategoryViewModel.cs` | MapFeatureItems, GetFeatures, RefreshFeatures |
| `src/client/Telemart.Client/ViewModels/TradeInSegment/TradeInSegmentCategorySettingsFeatureViewItem.cs` | TradeInSegmentCategorySettingsFeatureViewItem |
| `src/client/Telemart.Client/ViewModels/TradeIn/TraderInCoefViewItem.cs` | TraderInCoefViewItem |
| `src/client/Telemart.Client/ViewModels/TradeIn/TradeInCoefViewModel.cs` | HandleLoadedAsync |
| `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseCategoryHistoryViewItem.cs` | ShowcaseCategoryHistoryViewItem |
| `src/client/Telemart.Client/ViewModels/Segment/SegmentCategorySettingsFeatureViewItem.cs` | SegmentCategorySettingsFeatureViewItem |
| `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeViewModel.cs` | AddBundleCategory |

## Entry Points

Start here when exploring this area:

- **`TradeInSegmentCategorySettingsFeatureViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/TradeInSegment/TradeInSegmentCategorySettingsFeatureViewItem.cs:5`
- **`TraderInCoefViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/TradeIn/TraderInCoefViewItem.cs:6`
- **`ShowcaseCategoryHistoryViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseCategoryHistoryViewItem.cs:5`
- **`SegmentCategorySettingsFeatureViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Segment/SegmentCategorySettingsFeatureViewItem.cs:5`
- **`PromoCodeBundleCategoryViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeBundleCategoryViewItem.cs:7`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `TradeInSegmentCategorySettingsFeatureViewItem` | Class | `src/client/Telemart.Client/ViewModels/TradeInSegment/TradeInSegmentCategorySettingsFeatureViewItem.cs` | 5 |
| `TraderInCoefViewItem` | Class | `src/client/Telemart.Client/ViewModels/TradeIn/TraderInCoefViewItem.cs` | 6 |
| `ShowcaseCategoryHistoryViewItem` | Class | `src/client/Telemart.Client/ViewModels/Showcase/ShowcaseCategoryHistoryViewItem.cs` | 5 |
| `SegmentCategorySettingsFeatureViewItem` | Class | `src/client/Telemart.Client/ViewModels/Segment/SegmentCategorySettingsFeatureViewItem.cs` | 5 |
| `PromoCodeBundleCategoryViewItem` | Class | `src/client/Telemart.Client/ViewModels/PromoCode/PromoCodeBundleCategoryViewItem.cs` | 7 |
| `CreditOfferViewItem` | Class | `src/client/Telemart.Client/ViewModels/Payments/CreditOfferViewItem.cs` | 6 |
| `TelemartViewItemBase` | Class | `src/client/Telemart.Client/ViewModels/Base/TelemartViewItemBase.cs` | 5 |
| `AssemblyServiceProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/AssemblyService/AssemblyServiceProductViewItem.cs` | 8 |
| `SelectAssembledComputerRuleProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/AssembledComputerRule/SelectAssembledComputerRuleProductViewItem.cs` | 4 |
| `GeneratedAssembledComputerRuleProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/AssembledComputerRule/GeneratedAssembledComputerRuleProductViewItem.cs` | 5 |
| `AssembledComputerRuleCategoryViewItem` | Class | `src/client/Telemart.Client/ViewModels/AssembledComputerRule/AssembledComputerRuleCategoryViewItem.cs` | 8 |
| `AdditionalServiceProductsViewItem` | Class | `src/client/Telemart.Client/ViewModels/AdditionalServiceProduct/AdditionalServiceProductsViewItem.cs` | 8 |
| `AdditionalServiceProductSnViewItem` | Class | `src/client/Telemart.Client/ViewModels/AdditionalServiceProduct/AdditionalServiceProductSnViewItem.cs` | 8 |
| `ValueWrapper` | Class | `src/client/Telemart.Client/Style/Editors/ValueWrapper.cs` | 5 |
| `MultiComboBoxValue` | Class | `src/client/Telemart.Client/Style/Editors/MultiComboBoxValue.cs` | 8 |
| `ComboBoxValue` | Class | `src/client/Telemart.Client/Style/Editors/ComboBoxValue.cs` | 7 |
| `SelectMovementViewItem` | Class | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/SelectMovementViewItem.cs` | 4 |
| `MovementDelayProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/Warehouse/Movement/MovementDelayProductViewItem.cs` | 7 |
| `ReturnInvoiceProductConfirmViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/ReturnInvoice/ReturnInvoiceProductConfirmViewItem.cs` | 5 |
| `InvoiceDelayProductViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Invoice/InvoiceDelayProductViewItem.cs` | 7 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Order | 4 calls |
| Movement | 1 calls |
| Create | 1 calls |
| Complaint | 1 calls |
| PromoCode | 1 calls |
| Call | 1 calls |

## How to Explore

1. `gitnexus_context({name: "TradeInSegmentCategorySettingsFeatureViewItem"})` — see callers and callees
2. `gitnexus_query({query: "assembledcomputerrule"})` — find related execution flows
3. Read key files listed above for implementation details
