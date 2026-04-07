---
name: viewmodels
description: "Skill for the ViewModels area of telemart_soft_test. 56 symbols across 15 files."
---

# ViewModels

56 symbols | 15 files | Cohesion: 71%

## When to Use

- Working with code in `src/`
- Understanding how CustomMessageBoxViewModel, Create, ShowModuleInternal work
- Modifying viewmodels-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `src/client/Telemart.Client/ViewModels/MainWindowViewModel.cs` | HandleLoadedAsync, Navigate, LogoutAsync, StopJobsAsync, LockAsync (+12) |
| `src/client/Telemart.Client/ViewModels/WorkspaceViewModel.cs` | OnOutcomingCallAsync, ShowValidationResultView, ShowModuleInternal, FindDocumentByIdOrCreate, OnWikiHelpOpen (+10) |
| `src/client/Telemart.Client/ViewModels/WatsNewViewModel.cs` | HandleLoadedAsync, LoadEmployeeAcountInfoAsync, CreateTelewikiAccountAsync, GetCookieWikiAsync, NavigationStartingAsync (+1) |
| `src/client/Telemart.Client/ViewModels/SendSmsViewModel.cs` | TemplateChanged, CalculateCreditOffersAsync, ClearSelectedTemplate |
| `src/client/Telemart.Client/ViewModels/MetabaseViewModel.cs` | NavigationStartingAsync, SetCookieInWebView2 |
| `src/client/Telemart.Client/ViewModels/Complaint/ComplaintCreateViewModel.cs` | HandleLoadedAsync, QueryEmployees |
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs` | EditCreatedByAsync, EditProductAddedByAsync |
| `src/client/Telemart.Client/Common/Navigation/NavigationMenuItem.cs` | GetViewType, GetParameter |
| `src/client/Telemart.Client/ViewModels/Store/Order/OrderPaymentRecordViewItem.cs` | Create |
| `src/client/Telemart.Client/ViewModels/TradeIn/TradeInEditViewModel.cs` | LoadEmployeesAsync |

## Entry Points

Start here when exploring this area:

- **`CustomMessageBoxViewModel`** (Class) — `src/client/Telemart.Client/ViewModels/CustomMessageBoxViewModel.cs:11`
- **`Create`** (Method) — `src/client/Telemart.Client/ViewModels/Store/Order/OrderPaymentRecordViewItem.cs:57`
- **`ShowModuleInternal`** (Method) — `src/client/Telemart.Client/ViewModels/WorkspaceViewModel.cs:314`
- **`GetViewType`** (Method) — `src/client/Telemart.Client/Common/Navigation/NavigationMenuItem.cs:116`
- **`GetParameter`** (Method) — `src/client/Telemart.Client/Common/Navigation/NavigationMenuItem.cs:121`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `CustomMessageBoxViewModel` | Class | `src/client/Telemart.Client/ViewModels/CustomMessageBoxViewModel.cs` | 11 |
| `Create` | Method | `src/client/Telemart.Client/ViewModels/Store/Order/OrderPaymentRecordViewItem.cs` | 57 |
| `ShowModuleInternal` | Method | `src/client/Telemart.Client/ViewModels/WorkspaceViewModel.cs` | 314 |
| `GetViewType` | Method | `src/client/Telemart.Client/Common/Navigation/NavigationMenuItem.cs` | 116 |
| `GetParameter` | Method | `src/client/Telemart.Client/Common/Navigation/NavigationMenuItem.cs` | 121 |
| `SetUserPassword` | Method | `src/client/Telemart.Client/Common/Messages/TelewikiParameter.cs` | 24 |
| `CloseDocuments` | Method | `src/client/Telemart.Client/ViewModels/WorkspaceViewModel.cs` | 271 |
| `HideDocuments` | Method | `src/client/Telemart.Client/ViewModels/WorkspaceViewModel.cs` | 298 |
| `ShowDocuments` | Method | `src/client/Telemart.Client/ViewModels/WorkspaceViewModel.cs` | 306 |
| `WaitForNewClientSendTokenAndShutdown` | Method | `src/client/Telemart.Client/SingleInstance/SingleInstanceAppProcessor.cs` | 63 |
| `ShowCustomMessageBox` | Method | `src/client/Telemart.Client/Common/Services/MessageFacadeServive.cs` | 76 |
| `QueryEmployees` | Function | `src/client/Telemart.Client/ViewModels/Complaint/ComplaintCreateViewModel.cs` | 219 |
| `NavigationStartingAsync` | Method | `src/client/Telemart.Client/ViewModels/MetabaseViewModel.cs` | 93 |
| `SetCookieInWebView2` | Method | `src/client/Telemart.Client/ViewModels/MetabaseViewModel.cs` | 154 |
| `HandleLoadedAsync` | Method | `src/client/Telemart.Client/ViewModels/MainWindowViewModel.cs` | 669 |
| `Navigate` | Method | `src/client/Telemart.Client/ViewModels/MainWindowViewModel.cs` | 724 |
| `LogoutAsync` | Method | `src/client/Telemart.Client/ViewModels/MainWindowViewModel.cs` | 736 |
| `StopJobsAsync` | Method | `src/client/Telemart.Client/ViewModels/MainWindowViewModel.cs` | 776 |
| `LockAsync` | Method | `src/client/Telemart.Client/ViewModels/MainWindowViewModel.cs` | 802 |
| `ScheduleCheckUpdateJobAsync` | Method | `src/client/Telemart.Client/ViewModels/MainWindowViewModel.cs` | 1167 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `OnUserLoggedIn → QueryCarriesAsync` | cross_community | 3 |
| `OnUserLoggedIn → QueryWorkPlaceTypesAsync` | cross_community | 3 |
| `OnUserLoggedIn → QueryPaymentsAsync` | cross_community | 3 |
| `OnUserLoggedIn → QueryWarrantiesAsync` | cross_community | 3 |
| `HandleLoadedAsync → QueryEmployees` | cross_community | 3 |

## Connected Areas

| Area | Connections |
|------|-------------|
| ServiceRequests | 5 calls |
| Order | 4 calls |
| Controls | 2 calls |
| ServiceRepairs | 2 calls |
| Telemart.Client.PosTerminal.Ingenico | 1 calls |
| Telemart.Client.PosTerminal.PrivatBank | 1 calls |
| Jobs | 1 calls |
| Category | 1 calls |

## How to Explore

1. `gitnexus_context({name: "CustomMessageBoxViewModel"})` — see callers and callees
2. `gitnexus_query({query: "viewmodels"})` — find related execution flows
3. Read key files listed above for implementation details
