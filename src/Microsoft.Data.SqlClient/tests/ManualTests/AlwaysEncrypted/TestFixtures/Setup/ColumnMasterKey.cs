﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Data.SqlClient.ManualTesting.Tests.AlwaysEncrypted.Setup
{
    public abstract class ColumnMasterKey : DbObject
    {
        protected ColumnMasterKey(string name) : base(name)
        {
        }

        protected string KeyStoreProviderName { get; set; }
        public abstract string KeyPath { get; }

        public override void Create(SqlConnection sqlConnection)
        {
            string sql =
                $@"CREATE COLUMN MASTER KEY [{Name}]
                    WITH (
                        KEY_STORE_PROVIDER_NAME = N'{KeyStoreProviderName}',
                        KEY_PATH = N'{KeyPath}'
                    );";

            using (SqlCommand command = sqlConnection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        public override void Drop(SqlConnection sqlConnection)
        {
            string sql = $"DROP COLUMN MASTER KEY [{Name}];";

            using (SqlCommand command = sqlConnection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
    }
}
