﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.Data.SqlClient.ManualTesting.Tests
{
    public static class Utf8SupportTest
    {
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsUTF8Supported))]
        public static void CheckSupportUtf8ConnectionProperty()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandText = "SELECT CONNECTIONPROPERTY('SUPPORT_UTF8')";
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Assert.Equal(1, reader.GetInt32(0));
                    }
                }
            }
        }
    }
}
