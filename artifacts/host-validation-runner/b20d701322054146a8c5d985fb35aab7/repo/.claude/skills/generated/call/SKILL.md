---
name: call
description: "Skill for the Call area of telemart_soft_test. 63 symbols across 18 files."
---

# Call

63 symbols | 18 files | Cohesion: 77%

## When to Use

- Working with code in `src/`
- Understanding how CallViewItem, CallDependencyViewItem, TelemartEditorLocalizer work
- Modifying call-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/Store/Call/OutcomingCallViewModel.cs` | OnInitializeInDesignMode, LinkComplaintAsync, LinkServiceRequestAsync, LinkOrderAsync, OnCallDependencyChanged (+7) |
| `src/client/Telemart.Client/ViewModels/Store/Call/OperatorsViewModel.cs` | HandleLoadedAsync, RefreshAsync, LoadEmployeesAsync, LoadOperatorsAsync, LoadBusySecondsAfterCallAsync (+5) |
| `src/client/Telemart.Client/ViewModels/Store/Call/StoreCallsViewModel.cs` | RefreshAsync, GroupCalls, RaiseProperties, CreateGroup, MapToParent (+3) |
| `src/client/Telemart.Client/ViewModels/Store/Call/StoreCallsFilterViewModel.cs` | GetCallFilteringItem, GetFilteringItem, RefreshAsync, RefreshCallTypesAsync, RefreshEmployeesAsync (+3) |
| `src/client/Telemart.Client/ViewModels/Dialogs/Call/CallDialogViewModel.cs` | OnClose, SetParameterAsync, StartCallAsync, EndCallAsync, CancelCall (+1) |
| `src/client/Telemart.Client/ViewModels/Store/Call/CreateCallViewModel.cs` | HandleOkAsync, HandleLoadedAsync, HandleCallTypeChangedAsync, RefreshContractors |
| `src/client/Telemart.Client/ViewModels/Store/Call/CallViewModel.cs` | HandleLoadedAsync, GetSummaryItems, RefreshSummaryItems |
| `src/client/Telemart.Client/ViewModels/Store/Call/ChangeCallStateViewModel.cs` | HandleLoadedAsync, LoadAsteriskStatusesAsync |
| `src/client/Telemart.Client/ViewModels/Store/Call/CallViewItem.cs` | CallViewItem |
| `src/client/Telemart.Client/ViewModels/Store/Call/CallDependencyViewItem.cs` | CallDependencyViewItem |

## Entry Points

Start here when exploring this area:

- **`CallViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Store/Call/CallViewItem.cs:18`
- **`CallDependencyViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Store/Call/CallDependencyViewItem.cs:4`
- **`TelemartEditorLocalizer`** (Class) — `src/client/Telemart.Client/Localizers/TelemartEditorLocalizer.cs:4`
- **`OperatorViewItem`** (Class) — `src/client/Telemart.Client/ViewModels/Store/Call/OperatorViewItem.cs:6`
- **`CallCreateDto`** (Class) — `src/client/Telemart.Client.TransferObjects/Call/CallCreateDto.cs:5`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `CallViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Call/CallViewItem.cs` | 18 |
| `CallDependencyViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Call/CallDependencyViewItem.cs` | 4 |
| `TelemartEditorLocalizer` | Class | `src/client/Telemart.Client/Localizers/TelemartEditorLocalizer.cs` | 4 |
| `OperatorViewItem` | Class | `src/client/Telemart.Client/ViewModels/Store/Call/OperatorViewItem.cs` | 6 |
| `CallCreateDto` | Class | `src/client/Telemart.Client.TransferObjects/Call/CallCreateDto.cs` | 5 |
| `OnClose` | Method | `src/client/Telemart.Client/ViewModels/Dialogs/Call/CallDialogViewModel.cs` | 110 |
| `SetParameterAsync` | Method | `src/client/Telemart.Client/ViewModels/Dialogs/Call/CallDialogViewModel.cs` | 125 |
| `GetCallFilteringItem` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/StoreCallsFilterViewModel.cs` | 202 |
| `GetFilteringItem` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/StoreCallsFilterViewModel.cs` | 319 |
| `RefreshAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/StoreCallsFilterViewModel.cs` | 172 |
| `CreateCallByOrderAsync` | Method | `src/client/Telemart.Client/Helpers/CallHelper.cs` | 24 |
| `OnInitializeInDesignMode` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/OutcomingCallViewModel.cs` | 369 |
| `LinkComplaintAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/OutcomingCallViewModel.cs` | 716 |
| `LinkServiceRequestAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/OutcomingCallViewModel.cs` | 759 |
| `LinkOrderAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/OutcomingCallViewModel.cs` | 802 |
| `OnCallDependencyChanged` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/OutcomingCallViewModel.cs` | 859 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/OperatorsViewModel.cs` | 91 |
| `RefreshAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/OperatorsViewModel.cs` | 154 |
| `LoadEmployeesAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/OperatorsViewModel.cs` | 161 |
| `LoadOperatorsAsync` | Method | `src/client/Telemart.Client/ViewModels/Store/Call/OperatorsViewModel.cs` | 173 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `CreateCallByOrderAsync → ValidationResultViewModelParameter` | cross_community | 5 |
| `CreateCallByOrderAsync → ConfirmViewModelParameter` | cross_community | 4 |
| `CreateCallByOrderAsync → ValidationResultItem` | cross_community | 4 |
| `CreateCallByOrderAsync → ShowValidationResultView` | cross_community | 4 |
| `CreateCallByOrderAsync → ShowNotificationWarning` | cross_community | 4 |

## Connected Areas

| Area | Connections |
|------|-------------|
| Order | 9 calls |
| TradeIn | 3 calls |
| Category | 2 calls |
| ServiceRequests | 2 calls |
| SingleInstance | 1 calls |
| AdditionalServiceProduct | 1 calls |
| Warehouse | 1 calls |

## How to Explore

1. `gitnexus_context({name: "CallViewItem"})` — see callers and callees
2. `gitnexus_query({query: "call"})` — find related execution flows
3. Read key files listed above for implementation details
