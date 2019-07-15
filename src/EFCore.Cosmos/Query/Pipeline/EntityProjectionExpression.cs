// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore.Cosmos.Query.Pipeline
{
    public class EntityProjectionExpression : Expression
    {
        private readonly IDictionary<IProperty, SqlExpression> _propertyExpressionsCache
            = new Dictionary<IProperty, SqlExpression>();
        private readonly IDictionary<INavigation, Expression> _navigationExpressionsCache
            = new Dictionary<INavigation, Expression>();

        public EntityProjectionExpression(IEntityType entityType, Expression accessExpression)
        {
            EntityType = entityType;
            AccessExpression = accessExpression;
            Name = (accessExpression as RootReferenceExpression)?.Alias
                   ?? (accessExpression as ObjectAccessExpression)?.Name;
        }

        public override ExpressionType NodeType => ExpressionType.Extension;
        public override Type Type => EntityType.ClrType;

        public virtual Expression AccessExpression { get; }
        public virtual IEntityType EntityType { get; }
        public virtual string Name { get; }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var accessExpression = visitor.Visit(AccessExpression);

            return accessExpression != AccessExpression
                ? new EntityProjectionExpression(EntityType, accessExpression)
                : this;
        }

        public SqlExpression BindProperty(IProperty property)
        {
            if (!EntityType.IsAssignableFrom(property.DeclaringEntityType)
                && !property.DeclaringEntityType.IsAssignableFrom(EntityType))
            {
                throw new InvalidOperationException(
                    $"Called EntityProjectionExpression.GetProperty() with incorrect IProperty. EntityType:{EntityType.DisplayName()}, Property:{property.Name}");
            }

            if (!_propertyExpressionsCache.TryGetValue(property, out var expression))
            {
                expression = new KeyAccessExpression(property, AccessExpression);
                _propertyExpressionsCache[property] = expression;
            }

            return expression;
        }

        public Expression BindNavigation(INavigation navigation)
        {
            if (!EntityType.IsAssignableFrom(navigation.DeclaringEntityType)
                && !navigation.DeclaringEntityType.IsAssignableFrom(EntityType))
            {
                throw new InvalidOperationException(
                    $"Called EntityProjectionExpression.GetNavigation() with incorrect INavigation. EntityType:{EntityType.DisplayName()}, Navigation:{navigation.Name}");
            }

            if (!_navigationExpressionsCache.TryGetValue(navigation, out var expression))
            {
                expression = new EntityProjectionExpression(
                    navigation.GetTargetType(),
                    new ObjectAccessExpression(navigation, AccessExpression));
                if (navigation.IsCollection())
                {
                    expression = new ArrayProjectionExpression((EntityProjectionExpression)expression);
                }

                _navigationExpressionsCache[navigation] = expression;
            }

            return expression;
        }

        public override bool Equals(object obj)
            => obj != null
               && (ReferenceEquals(this, obj)
                   || obj is EntityProjectionExpression entityProjectionExpression
                   && Equals(entityProjectionExpression));

        private bool Equals(EntityProjectionExpression entityProjectionExpression)
            => Equals(EntityType, entityProjectionExpression.EntityType)
               && AccessExpression.Equals(entityProjectionExpression.AccessExpression);

        public override int GetHashCode() => HashCode.Combine(EntityType, AccessExpression);
    }
}
