using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Mapper;

namespace ChannelAPI
{
    public class DapperFactory
    {
        public static string ConnectionString { get { return _connectionString; } set { if (string.IsNullOrEmpty(_connectionString)) { _connectionString = value; } } }
        private static string _connectionString;

        static DapperFactory()
        {
            
        }

        public static IDbConnection GetOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();

            return connection;
        }

        public static async Task<IDbConnection> GetOpenConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            return connection;
        }

        public static IEnumerable<T> Query<T>(IDbConnection connection, string strQuery)
        {
            var value = connection.Query<T>(strQuery);
            return value;
        }

        public static async Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string strQuery)
        {
            var value = await connection.QueryAsync<T>(strQuery);
            return value;
        }

        public static IEnumerable<T> Query<T,T2>(IDbConnection connection, string strQuery)
        {
            var value = connection.Query<T, T2>(strQuery);
            return value;
        }

        public static async Task<IEnumerable<T>> QueryAsync<T,T2>(IDbConnection connection, string strQuery)
        {
            var value = await connection.QueryAsync<T, T2>(strQuery);
            return value;
        }

        public static IEnumerable<T> Query<T,T2,T3> (IDbConnection connection, string strQuery)
        {
            var value = connection.Query<T, T2, T3>(strQuery);
            return value;
        }

        public static async Task<IEnumerable<T>> QueryAsync<T,T2,T3> (IDbConnection connection, string strQuery)
        {
            var value = await connection.QueryAsync<T, T2, T3>(strQuery);
            return value;
        }
    }
}
