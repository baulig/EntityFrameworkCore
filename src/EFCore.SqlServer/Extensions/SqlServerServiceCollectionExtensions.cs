// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Pipeline;
using Microsoft.EntityFrameworkCore.Relational.Query.Pipeline;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Pipeline;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Update.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.ValueGeneration.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.ValueGeneration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     SQL Server specific extension methods for <see cref="IServiceCollection" />.
    /// </summary>
    public static class SqlServerServiceCollectionExtensions
    {
        /// <summary>
        ///     <para>
        ///         Adds the services required by the Microsoft SQL Server database provider for Entity Framework
        ///         to an <see cref="IServiceCollection" />. You use this method when using dependency injection
        ///         in your application, such as with ASP.NET. For more information on setting up dependency
        ///         injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
        ///     </para>
        ///     <para>
        ///         You only need to use this functionality when you want Entity Framework to resolve the services it uses
        ///         from an external dependency injection container. If you are not using an external
        ///         dependency injection container, Entity Framework will take care of creating the services it requires.
        ///     </para>
        /// </summary>
        /// <example>
        ///     <code>
        ///            public void ConfigureServices(IServiceCollection services)
        ///            {
        ///                var connectionString = "connection string to database";
        ///
        ///                services
        ///                    .AddEntityFrameworkSqlServer()
        ///                    .AddDbContext&lt;MyContext&gt;((serviceProvider, options) =>
        ///                        options.UseSqlServer(connectionString)
        ///                               .UseInternalServiceProvider(serviceProvider));
        ///            }
        ///        </code>
        /// </example>
        /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <returns>
        ///     The same service collection so that multiple calls can be chained.
        /// </returns>
        public static IServiceCollection AddEntityFrameworkSqlServer([NotNull] this IServiceCollection serviceCollection)
        {
            Check.NotNull(serviceCollection, nameof(serviceCollection));

            var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
                .TryAdd<LoggingDefinitions, SqlServerLoggingDefinitions>()
                .TryAdd<IDatabaseProvider, DatabaseProvider<SqlServerOptionsExtension>>()
                .TryAdd<IValueGeneratorCache>(p => p.GetService<ISqlServerValueGeneratorCache>())
                .TryAdd<IRelationalTypeMappingSource, SqlServerTypeMappingSource>()
                .TryAdd<ISqlGenerationHelper, SqlServerSqlGenerationHelper>()
                .TryAdd<IMigrationsAnnotationProvider, SqlServerMigrationsAnnotationProvider>()
                .TryAdd<IModelValidator, SqlServerModelValidator>()
                .TryAdd<IProviderConventionSetBuilder, SqlServerConventionSetBuilder>()
                .TryAdd<IUpdateSqlGenerator>(p => p.GetService<ISqlServerUpdateSqlGenerator>())
                .TryAdd<IModificationCommandBatchFactory, SqlServerModificationCommandBatchFactory>()
                .TryAdd<IValueGeneratorSelector, SqlServerValueGeneratorSelector>()
                .TryAdd<IRelationalConnection>(p => p.GetService<ISqlServerConnection>())
                .TryAdd<IMigrationsSqlGenerator, SqlServerMigrationsSqlGenerator>()
                .TryAdd<IRelationalDatabaseCreator, SqlServerDatabaseCreator>()
                .TryAdd<IHistoryRepository, SqlServerHistoryRepository>()
                .TryAdd<ICompiledQueryCacheKeyGenerator, SqlServerCompiledQueryCacheKeyGenerator>()
                .TryAdd<IExecutionStrategyFactory, SqlServerExecutionStrategyFactory>()
                .TryAdd<ISingletonOptions, ISqlServerOptions>(p => p.GetService<ISqlServerOptions>())

                // New Query Pipeline
                .TryAdd<IMethodCallTranslatorProvider, SqlServerMethodCallTranslatorProvider>()
                .TryAdd<IMemberTranslatorProvider, SqlServerMemberTranslatorProvider>()
                .TryAdd<IQuerySqlGeneratorFactory, SqlServerQuerySqlGeneratorFactory>()
                .TryAdd<IShapedQueryOptimizerFactory, SqlServerShapedQueryOptimizerFactory>()
                .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, SqlServerSqlTranslatingExpressionVisitorFactory>()


                .TryAddProviderSpecificServices(
                    b => b
                        .TryAddSingleton<ISqlServerValueGeneratorCache, SqlServerValueGeneratorCache>()
                        .TryAddSingleton<ISqlServerOptions, SqlServerOptions>()
                        .TryAddSingleton<ISqlServerUpdateSqlGenerator, SqlServerUpdateSqlGenerator>()
                        .TryAddSingleton<ISqlServerSequenceValueGeneratorFactory, SqlServerSequenceValueGeneratorFactory>()
                        .TryAddScoped<ISqlServerConnection, SqlServerConnection>());

            builder.TryAddCoreServices();

            return serviceCollection;
        }
    }
}
