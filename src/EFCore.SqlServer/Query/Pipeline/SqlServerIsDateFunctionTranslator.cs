﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Relational.Query.Pipeline;
using Microsoft.EntityFrameworkCore.Relational.Query.Pipeline.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Pipeline
{
    public class SqlServerIsDateFunctionTranslator : IMethodCallTranslator
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        private static readonly MethodInfo _methodInfo = typeof(SqlServerDbFunctionsExtensions)
            .GetRuntimeMethod(nameof(SqlServerDbFunctionsExtensions.IsDate), new[] { typeof(DbFunctions), typeof(string) });

        public SqlServerIsDateFunctionTranslator(ISqlExpressionFactory sqlExpressionFactory)
            => _sqlExpressionFactory = sqlExpressionFactory;

        public SqlExpression Translate(SqlExpression instance, MethodInfo method, IList<SqlExpression> arguments)
        {
            return _methodInfo.Equals(method)
                ? _sqlExpressionFactory.Convert(
                        _sqlExpressionFactory.Function(
                        "ISDATE",
                        new[]
                        {
                            arguments[1]
                        },
                        _methodInfo.ReturnType),
                  _methodInfo.ReturnType)
                : null;
        }
    }
}
