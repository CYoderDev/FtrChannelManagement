using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.Data.Objects;

namespace FrontierVOps.Data
{
    public class DBFactory
    {
        #region ExecuteReader
        /// <summary>
        /// Creates an SQL connection and reads data records
        /// </summary>
        /// <param name="InstanceName">Name of the SQL instance</param>
        /// <param name="DBName">Name of the SQL database</param>
        /// <param name="CommandString">Query or stored procedure to execute</param>
        /// <param name="CmdType">Type of command string used, text or stored procedure</param>
        /// <param name="Parameters">Parameters to pass to the stored procedure (ignored if Command Type is text)</param>
        /// <param name="Username">Username with minimum read access to the database</param>
        /// <param name="Password">Password for the user</param>
        /// <param name="Timeout">(Optional) Connection timeout value</param>
        /// <returns>Data records returned from the query</returns>
        public static IEnumerable<IDataRecord> SQL_ExecuteReader(string InstanceName, string DBName, string CommandString, CommandType CmdType, Tuple<string, object>[] Parameters, string Username, SecureString Password, int Timeout = 30)
        {
            var sqlDB = new SqlDb();
            var sqlDS = new Datasource(InstanceName);
            sqlDB.DatabaseName = DBName;
            sqlDB.Username = Username;
            sqlDB.Password = Password;

            return SQL_ExecuteReader(sqlDB.CreateConnectionString(sqlDS), CommandString, CmdType);
        }

        /// <summary>
        /// Creates an SQL connection and reads data records
        /// </summary>
        /// <param name="InstanceName">Name of the SQL instance</param>
        /// <param name="DBName">Name of the SQL database</param>
        /// <param name="CommandString">Query or stored procedure to execute</param>
        /// <param name="CmdType">Type of command string used, text or stored procedure</param>
        /// <param name="Parameters">Parameters to pass to the stored procedure (ignored if Command Type is text)</param>
        /// <param name="Timeout">(Optional) Connection timeout value</param>
        /// <returns>Data records returned from the query</returns>
        public static IEnumerable<IDataRecord> SQL_ExecuteReader(string InstanceName, string DBName, string CommandString, CommandType CmdType, Tuple<string, object>[] Parameters, int Timeout = 30)
        {
            var sqlDB = new SqlDb();
            var sqlDS = new Datasource(InstanceName);
            sqlDB.DatabaseName = DBName;
            return SQL_ExecuteReader(sqlDB.CreateConnectionString(sqlDS), CommandString, CmdType);
        }

        /// <summary>
        /// Creates an SQL connection and reads data records
        /// </summary>
        /// <param name="ConnectionString">Connection details to connect to a sql database server</param>
        /// <param name="CommandString">Query or stored procedure to execute</param>
        /// <param name="CmdType">Type of command string used, text or stored procedure</param>
        /// <returns>Data records returned from the query</returns>
        public static IEnumerable<IDataRecord> SQL_ExecuteReader(string ConnectionString, string CommandString, CommandType CmdType)
        {
            return SQL_ExecuteReader(ConnectionString, CommandString, CmdType, null);
        }

        /// <summary>
        /// Creates an SQL connection and reads data records
        /// </summary>
        /// <param name="ConnectionString">Connection details to connect to a sql database server</param>
        /// <param name="CommandString">Query or stored procedure</param>
        /// <param name="CmdType">Type of command string used, text or stored procedure</param>
        /// <param name="Parameters">Parameters to pass to the stored procedure (ignored if Command Type is text)</param>
        /// <returns>Data records returned from the query</returns>
        public static IEnumerable<IDataRecord> SQL_ExecuteReader(string ConnectionString, string CommandString, CommandType CmdType, Tuple<string, object>[] Parameters)
        {
            Parameters = Parameters ?? new Tuple<string, object>[0];

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandType = CmdType;
                command.CommandText = CommandString;

                for (int i = 0; i < Parameters.Length; i++)
                {
                    command.Parameters.AddWithValue(Parameters[i].Item1, Parameters[i].Item2);
                }

                connection.Open();

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader;
                    }
                }
            }
        }

        public static async Task<IEnumerable<IDataRecord>> SQL_ExecuteReaderAsync(string ConnectionString, string CommandString, CommandType CmdType, Tuple<string, object>[] Parameters)
        {
            Parameters = Parameters ?? new Tuple<string, object>[0];

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandType = CmdType;
                command.CommandText = CommandString;

                for (int i = 0; i < Parameters.Length; i++)
                {
                    command.Parameters.AddWithValue(Parameters[i].Item1, Parameters[i].Item2);
                }

                await connection.OpenAsync();

                List<IDataRecord> allRecords = new List<IDataRecord>();

                using (IDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        allRecords.Add(reader);
                    }
                    return allRecords;
                }
            }
        }

        public static async Task SQL_ExecuteReaderAsync(string ConnectionString, string CommandString, CommandType CmdType, Tuple<string, object>[] Parameters, Action<IDataReader> fn)
        {
            Parameters = Parameters ?? new Tuple<string, object>[0];

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandType = CmdType;
                command.CommandText = CommandString;

                for (int i = 0; i < Parameters.Length; i++)
                {
                    command.Parameters.AddWithValue(Parameters[i].Item1, Parameters[i].Item2);
                }

                await connection.OpenAsync();

                using (IDataReader reader = await command.ExecuteReaderAsync())
                {
                    fn(reader);
                }
            }
        }
        #endregion //ExecuteReader

        #region ExecuteNonQuery
        /// <summary>
        /// Executes a single SQL non query for updating or inserting data into a table
        /// </summary>
        /// <param name="InstanceName">Name of the SQL instance</param>
        /// <param name="DBName">Name of the SQL database</param>
        /// <param name="CommandString">Query or stored procedure to execute</param>
        /// <param name="CmdType">Type of command string used, text or stored procedure</param>
        /// <param name="Parameters">Parameters to pass to the stored procedure (ignored if Command Type is text)</param>
        /// <param name="UserName">Username with minimum read access to the database</param>
        /// <param name="Password">Password for the user</param>
        /// <param name="Timeout">(Optional) Connection timeout value</param>
        /// <returns>The number of rows affected</returns>
        public static int SQL_ExecuteNonQuery(string InstanceName, string DBName, string CommandString, CommandType CmdType, Tuple<string, object>[] Parameters, string UserName, SecureString Password, int Timeout = 30)
        {
            var sqlDB = new SqlDb();
            var sqlDS = new Datasource(InstanceName);
            sqlDB.DatabaseName = DBName;
            sqlDB.Username = UserName;
            sqlDB.Password = Password;
            sqlDB.ConnectionTimeout = Timeout;
            return (SQL_ExecuteNonQuery(sqlDB.CreateConnectionString(sqlDS), CommandString, CmdType, Parameters));
        }

        /// <summary>
        /// Executes a single SQL non query for updating or inserting data into a table
        /// </summary>
        /// <param name="InstanceName">Name of the SQL instance</param>
        /// <param name="DBName">Name of the SQL database</param>
        /// <param name="CommandString">Query or stored procedure to execute</param>
        /// <param name="CmdType">Type of command string used, text or stored procedure</param>
        /// <param name="Parameters">Parameters to pass to the stored procedure (ignored if Command Type is text)</param>
        /// <param name="Timeout">(Optional) Connection timeout value</param>
        /// <returns>The number of rows affected</returns>
        public static int SQL_ExecuteNonQuery(string InstanceName, string DBName, string CommandString, CommandType CmdType, Tuple<string, object>[] Parameters, int Timeout = 30)
        {
            var sqlDB = new SqlDb();
            var sqlDS = new Datasource(InstanceName);
            sqlDB.DatabaseName = DBName;
            sqlDB.ConnectionTimeout = Timeout;
            return (SQL_ExecuteNonQuery(sqlDB.CreateConnectionString(sqlDS), CommandString, CmdType, Parameters));
        }

        /// <summary>
        /// Executes a single SQL non query for updating or inserting data into a table
        /// </summary>
        /// <param name="ConnectionString">Connection details to connect to a sql database server</param>
        /// <param name="CommandString">Query or stored procedure to execute</param>
        /// <param name="CmdType">Type of command string used, text or stored procedure</param>
        /// <returns>The number of rows affected</returns>
        public static int SQL_ExecuteNonQuery(string ConnectionString, string CommandString, CommandType CmdType)
        {
            return SQL_ExecuteNonQuery(ConnectionString, CommandString, CmdType, null);
        }

        /// <summary>
        /// Executes a single SQL non query for updating or inserting data into a table
        /// </summary>
        /// <param name="ConnectionString">Connection details to connect to a sql database server</param>
        /// <param name="CommandString">Query or stored procedure to execute</param>
        /// <param name="CmdType">Type of command string used, text or stored procedure</param>
        /// <param name="Parameters">Parameters (Name/Value) to pass to the stored procedure (ignored if Command Type is text)</param>
        /// <returns>The number of rows affected</returns>
        public static int SQL_ExecuteNonQuery(string ConnectionString, string CommandString, CommandType CmdType, Tuple<string, object>[] Parameters)
        {
            Parameters = Parameters ?? new Tuple<string, object>[0];

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                using (SqlCommand command = connection.CreateCommand())
                {
                    try
                    {
                        command.CommandType = CmdType;
                        command.CommandText = CommandString;
                        command.Transaction = transaction;

                        if (CmdType != CommandType.Text)
                        {
                            for (int i = 0; i < Parameters.Length; i++)
                            {
                                command.Parameters.AddWithValue(Parameters[i].Item1, Parameters[i].Item2);
                            }
                        }

                        int retVal = command.ExecuteNonQuery();

                        transaction.Commit();
                        return retVal;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public static async Task<int> SQL_ExecuteNonQueryAsync(string ConnectionString, string CommandString, CommandType CmdType, Tuple<string, object>[] Parameters)
        {
            Parameters = Parameters ?? new Tuple<string, object>[0];

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                using (SqlCommand command = connection.CreateCommand())
                {
                    try
                    {
                        command.CommandType = CmdType;
                        command.CommandText = CommandString;
                        command.Transaction = transaction;

                        if (CmdType != CommandType.Text)
                        {
                            for (int i = 0; i < Parameters.Length; i++)
                            {
                                command.Parameters.AddWithValue(Parameters[i].Item1, Parameters[i].Item2);
                            }
                        }

                        int retVal = await command.ExecuteNonQueryAsync();

                        transaction.Commit();
                        return retVal;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
        }

        /// <summary>
        /// Executes multiple T-SQL non-query commands
        /// </summary>
        /// <param name="ConnectionString">Connection details to connect to a sql database server</param>
        /// <param name="CommandStrings">Dictionary with the command string as the key, and optional parameters (Name/Value) as the value. Params are ignored if command type is text</param>
        /// <param name="CmdType">Type of command string used, text or stored procedure</param>
        /// <returns>The number of rows affected</returns>
        public static int SQL_ExecuteNonQuery(string ConnectionString, IDictionary<string,Tuple<string,object>[]> CommandStrings, CommandType CmdType)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                //Create sql transaction
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int retVal = 0;

                        //Create sql commands and execute non reader for each
                        foreach (var CmdStr in CommandStrings)
                        {
                            using (SqlCommand command = new SqlCommand(CmdStr.Key, connection, transaction))
                            {
                                //If sproc, add parameters to command
                                if (CmdType == CommandType.StoredProcedure)
                                {
                                    //if no parameters for the command, create one to prevent null argument exception
                                    var Parameters = CmdStr.Value ?? new Tuple<string, object>[0];

                                    for (int i = 0; i < Parameters.Length; i++)
                                        command.Parameters.AddWithValue(Parameters[i].Item1, Parameters[i].Item2);
                                }

                                retVal += command.ExecuteNonQuery();
                            }
                        }

                        //Commit the transaction to perform all together
                        transaction.Commit();

                        return retVal;
                    }
                    catch (Exception ex)
                    {
                        //Rollback transaction to prevent db corruption or instability and rethrow exception
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
        }
        #endregion //ExecuteNonQuery
    }
}
