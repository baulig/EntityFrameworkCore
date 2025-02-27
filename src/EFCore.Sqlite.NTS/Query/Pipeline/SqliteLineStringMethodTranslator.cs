﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Relational.Query.Pipeline;
using Microsoft.EntityFrameworkCore.Relational.Query.Pipeline.SqlExpressions;
using NetTopologySuite.Geometries;

namespace Microsoft.EntityFrameworkCore.Sqlite.Query.Pipeline
{
    public class SqliteLineStringMethodTranslator : IMethodCallTranslator
    {
        private static readonly MethodInfo _getPointN
            = typeof(LineString).GetRuntimeMethod(nameof(LineString.GetPointN), new[] { typeof(int) });
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        public SqliteLineStringMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        public SqlExpression Translate(SqlExpression instance, MethodInfo method, IList<SqlExpression> arguments)
        {
            if (Equals(method, _getPointN))
            {
                return _sqlExpressionFactory.Function(
                    "PointN",
                    new SqlExpression[] {
                        instance,
                        _sqlExpressionFactory.Add(
                            arguments[0],
                            _sqlExpressionFactory.Constant(1))
                    },
                    method.ReturnType);
            }

            return null;
        }
    }
}
