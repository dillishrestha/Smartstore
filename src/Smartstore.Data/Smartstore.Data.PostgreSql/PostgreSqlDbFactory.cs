﻿using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql;
using Smartstore.Data.Providers;

namespace Smartstore.Data.PostgreSql
{
    internal class PostgreSqlDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.PostgreSql;

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString)
            => new NpgsqlConnectionStringBuilder(connectionString);

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(
            string server,
            string database,
            string userId,
            string password)
        {
            Guard.NotEmpty(server);

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = server,
                Database = database,
                Username = userId,
                Password = password,
                Pooling = true,
                MinPoolSize = 1,
                MaxPoolSize = 1024, 
                Multiplexing = false
            };

            return builder;
        }

        public override DataProvider CreateDataProvider(DatabaseFacade database)
            => new PostgreSqlDataProvider(database);

        public override TContext CreateDbContext<TContext>(string connectionString, int? commandTimeout = null)
        {
            Guard.NotEmpty(connectionString);

            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .UseNpgsql(connectionString, sql =>
                {
                    sql.CommandTimeout(commandTimeout);
                })
                .ReplaceService<IMethodCallTranslatorProvider, PostgreSqlMappingMethodCallTranslatorProvider>();

            return (TContext)Activator.CreateInstance(typeof(TContext), new object[] { optionsBuilder.Options });
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {   
            return builder.UseNpgsql(connectionString, sql =>
            {
                var extension = builder.Options.FindExtension<DbFactoryOptionsExtension>();

                if (extension != null)
                {
                    if (extension.CommandTimeout.HasValue)
                        sql.CommandTimeout(extension.CommandTimeout.Value);

                    if (extension.MinBatchSize.HasValue)
                        sql.MinBatchSize(extension.MinBatchSize.Value);

                    if (extension.MaxBatchSize.HasValue)
                        sql.MaxBatchSize(extension.MaxBatchSize.Value);

                    if (extension.QuerySplittingBehavior.HasValue)
                        sql.UseQuerySplittingBehavior(extension.QuerySplittingBehavior.Value);

                    if (extension.UseRelationalNulls.HasValue)
                        sql.UseRelationalNulls(extension.UseRelationalNulls.Value);
                }
            })
            .ReplaceService<IMethodCallTranslatorProvider, PostgreSqlMappingMethodCallTranslatorProvider>();
        }
    }
}