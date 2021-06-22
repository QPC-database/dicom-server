﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Numerics;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Indexing;
using Microsoft.Health.Dicom.SqlServer.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Store;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Polly;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class SqlDataStoreTestsFixture : IAsyncLifetime
    {
        private const string LocalConnectionString = "server=(local);Integrated Security=true";

        private readonly string _masterConnectionString;
        private readonly string _databaseName;
        private readonly SchemaInitializer _schemaInitializer;

        // Only 1 public constructor is allowed for test fixture.
        internal SqlDataStoreTestsFixture(string databaseName)
        {
            EnsureArg.IsNotNullOrEmpty(databaseName, nameof(databaseName));
            _databaseName = databaseName;
            string initialConnectionString = Environment.GetEnvironmentVariable("SqlServer:ConnectionString") ?? LocalConnectionString;
            _masterConnectionString = new SqlConnectionStringBuilder(initialConnectionString) { InitialCatalog = "master" }.ToString();
            TestConnectionString = new SqlConnectionStringBuilder(initialConnectionString) { InitialCatalog = _databaseName }.ToString();

            var config = new SqlServerDataStoreConfiguration
            {
                ConnectionString = TestConnectionString,
                Initialize = true,
                SchemaOptions = new SqlServerSchemaOptions
                {
                    AutomaticUpdatesEnabled = true,
                },
            };

            var scriptProvider = new ScriptProvider<SchemaVersion>();

            var baseScriptProvider = new BaseScriptProvider();

            var mediator = Substitute.For<IMediator>();

            var sqlConnectionStringProvider = new DefaultSqlConnectionStringProvider(config);

            var sqlConnectionFactory = new DefaultSqlConnectionFactory(sqlConnectionStringProvider);

            var schemaManagerDataStore = new SchemaManagerDataStore(sqlConnectionFactory);

            var schemaUpgradeRunner = new SchemaUpgradeRunner(scriptProvider, baseScriptProvider, NullLogger<SchemaUpgradeRunner>.Instance, sqlConnectionFactory, schemaManagerDataStore);

            var schemaInformation = new SchemaInformation(SchemaVersionConstants.Min, SchemaVersionConstants.Max);

            _schemaInitializer = new SchemaInitializer(config, schemaUpgradeRunner, schemaInformation, sqlConnectionFactory, sqlConnectionStringProvider, mediator, NullLogger<SchemaInitializer>.Instance);

            SqlTransactionHandler = new SqlTransactionHandler();

            SqlConnectionWrapperFactory = new SqlConnectionWrapperFactory(SqlTransactionHandler, new SqlCommandWrapperFactory(), sqlConnectionFactory);

            var schemaResolver = new BackgroundSchemaVersionResolver(schemaInformation);

            IndexDataStoreFactory = new SqlStoreFactory<ISqlIndexDataStore, IIndexDataStore>(
                schemaResolver,
                new[]
                {
                    new SqlIndexDataStoreV1(SqlConnectionWrapperFactory),
                    new SqlIndexDataStoreV2(SqlConnectionWrapperFactory),
                    new SqlIndexDataStoreV3(SqlConnectionWrapperFactory),
                    new SqlIndexDataStoreV4(SqlConnectionWrapperFactory)
                });

            InstanceStore = new SqlInstanceStore(SqlConnectionWrapperFactory);

            ExtendedQueryTagStoreFactory = new SqlStoreFactory<ISqlExtendedQueryTagStore, IExtendedQueryTagStore>(
                schemaResolver,
                new[]
                {
                    new SqlExtendedQueryTagStoreV1(),
                    new SqlExtendedQueryTagStoreV2(SqlConnectionWrapperFactory, NullLogger<SqlExtendedQueryTagStoreV2>.Instance),
                    new SqlExtendedQueryTagStoreV3(SqlConnectionWrapperFactory, NullLogger<SqlExtendedQueryTagStoreV3>.Instance),
                    new SqlExtendedQueryTagStoreV4(SqlConnectionWrapperFactory, NullLogger<SqlExtendedQueryTagStoreV4>.Instance)
                });

            ReindexStateStore = new SqlReindexStateStore(SqlConnectionWrapperFactory);

            SqlIndexDataStoreTestHelper = new SqlIndexDataStoreTestHelper(TestConnectionString);
            ReindexStateStoreTestHelper = new ReindexStateStoreTestHelper(TestConnectionString);
            ExtendedQueryTagStoreTestHelper = new ExtendedQueryTagStoreTestHelper(TestConnectionString);
        }

        public SqlDataStoreTestsFixture()
            : this(GenerateDatabaseName())
        {
        }

        public SqlTransactionHandler SqlTransactionHandler { get; }

        public SqlConnectionWrapperFactory SqlConnectionWrapperFactory { get; }

        public IStoreFactory<IIndexDataStore> IndexDataStoreFactory { get; }

        public string TestConnectionString { get; }

        public IInstanceStore InstanceStore { get; }

        public IStoreFactory<IExtendedQueryTagStore> ExtendedQueryTagStoreFactory { get; }

        public IReindexStateStore ReindexStateStore { get; }

        public SqlIndexDataStoreTestHelper SqlIndexDataStoreTestHelper { get; }

        public IReindexStateStoreTestHelper ReindexStateStoreTestHelper { get; }

        public IExtendedQueryTagStoreTestHelper ExtendedQueryTagStoreTestHelper { get; }

        public static string GenerateDatabaseName(string prefix = "DICOMINTEGRATIONTEST_")
        {
            return $"{prefix}{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{BigInteger.Abs(new BigInteger(Guid.NewGuid().ToByteArray()))}";
        }

        public async Task InitializeAsync(bool forceIncrementalSchemaUpgrade)
        {
            // Create the database
            using (var sqlConnection = new SqlConnection(_masterConnectionString))
            {
                await sqlConnection.OpenAsync();

                using (SqlCommand command = sqlConnection.CreateCommand())
                {
                    command.CommandTimeout = 600;
                    command.CommandText = $"CREATE DATABASE {_databaseName}";
                    await command.ExecuteNonQueryAsync();
                }
            }

            // verify that we can connect to the new database. This sometimes does not work right away with Azure SQL.
            await Policy
                .Handle<SqlException>()
                .WaitAndRetryAsync(
                    retryCount: 7,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .ExecuteAsync(async () =>
                {
                    using (var sqlConnection = new SqlConnection(TestConnectionString))
                    {
                        await sqlConnection.OpenAsync();
                        using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                        {
                            sqlCommand.CommandText = "SELECT 1";
                            await sqlCommand.ExecuteScalarAsync();
                        }
                    }
                });

            await _schemaInitializer.InitializeAsync(forceIncrementalSchemaUpgrade);
        }

        public Task InitializeAsync()
        {
            return InitializeAsync(forceIncrementalSchemaUpgrade: false);
        }

        public async Task DisposeAsync()
        {
            using (var sqlConnection = new SqlConnection(_masterConnectionString))
            {
                await sqlConnection.OpenAsync();
                SqlConnection.ClearAllPools();
                await using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandTimeout = 600;
                    sqlCommand.CommandText = $"DROP DATABASE IF EXISTS {_databaseName}";
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
