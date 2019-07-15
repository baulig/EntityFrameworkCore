// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Cosmos.Query.Pipeline
{
    public class ArrayProjectionExpression : Expression
    {
        public ArrayProjectionExpression(EntityProjectionExpression entityExpression)
        {
            Type = typeof(IEnumerable<>).MakeGenericType(entityExpression.Type);
            EntityExpression = entityExpression;
        }

        public override Type Type { get; }
        public virtual EntityProjectionExpression EntityExpression { get; }
        public virtual string Name => EntityExpression.Name;

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var expression = visitor.Visit(EntityExpression);

            return Update((EntityProjectionExpression)expression);
        }

        public ArrayProjectionExpression Update(EntityProjectionExpression entityExpression)
            => entityExpression != EntityExpression
                ? new ArrayProjectionExpression(entityExpression)
                : this;

        public override bool Equals(object obj)
            => obj != null
            && (ReferenceEquals(this, obj)
                || obj is ArrayProjectionExpression arrayProjectionExpression
                    && Equals(arrayProjectionExpression));

        private bool Equals(ArrayProjectionExpression arrayProjectionExpression)
            => EntityExpression.Equals(arrayProjectionExpression.EntityExpression);

        public override int GetHashCode() => EntityExpression.GetHashCode();
    }
}
