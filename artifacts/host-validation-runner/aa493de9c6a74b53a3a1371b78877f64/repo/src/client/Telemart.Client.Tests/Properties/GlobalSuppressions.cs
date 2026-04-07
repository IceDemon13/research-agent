//// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
//// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "AsyncUsage.CSharp.Naming",
    "UseAsyncSuffix:Use Async suffix",
    Justification = "Not nedded in tests",
    Scope = "member",
    Target = "~T:Telemart.Client.Tests.OrderViewModelTests")]