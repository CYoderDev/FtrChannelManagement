using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.Data.Objects;

namespace FrontierVOps.Data
{
    public class DBFactory
    {

        /// <summary>
        /// Creates an SQL connection and reads data records
        /// </summary>
        /// <param name="InstanceName">Name of the SQL instance</param>
        /// <param name="DBName">Name of the SQL database</param>
        /// <param name="CommandString">Query or stored procedure to execute</param>
        /// <param name="CmdType">Type of command string used, text or stored procedure</param>
        /// <returns>Data records returned from the query</returns>
        public static IEnumerable<IDataRecord> SQL_ExecuteReader(string InstanceName, string DBName, string CommandString, CommandType CmdType)
        {
            var sqlDB = new SqlDb();
            sqlDB.DataSource = InstanceName;
            sqlDB.DatabaseName = DBName;
            return SQL_ExecuteReader(sqlDB.CreateConnectionString(), CommandString, CmdType);
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
    }
}
