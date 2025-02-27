﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace Microsoft.EntityFrameworkCore
{
    public class SqliteComplianceTest : RelationalComplianceTestBase
    {
        protected override ICollection<Type> IgnoredTestBases { get; } = new HashSet<Type>
        {
            typeof(FromSqlSprocQueryTestBase<>),
            typeof(SqlExecutorTestBase<>),
            typeof(UdfDbFunctionTestBase<>),
            typeof(ComplexNavigationsWeakQueryTestBase<>), // issue #15285
            typeof(FiltersInheritanceTestBase<>),          // issue #15264
            typeof(FiltersTestBase<>),                     // issue #15264
            typeof(OwnedQueryTestBase<>),                  // issue #15285
            typeof(QueryFilterFuncletizationTestBase<>),   // issue #15264
            typeof(RelationalOwnedQueryTestBase<>),        // issue #15285
            // Query pipeline
            typeof(ConcurrencyDetectorTestBase<>),
            typeof(CompiledQueryTestBase<>),
            typeof(ConcurrencyDetectorRelationalTestBase<>),
            typeof(QueryNoClientEvalTestBase<>),
            typeof(WarningsTestBase<>),
        };

        protected override Assembly TargetAssembly { get; } = typeof(SqliteComplianceTest).Assembly;
    }
}
