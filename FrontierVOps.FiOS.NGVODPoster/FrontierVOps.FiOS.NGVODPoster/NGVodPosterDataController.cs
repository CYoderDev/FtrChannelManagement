using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastMember;
using FrontierVOps.Data;

namespace FrontierVOps.FiOS.NGVODPoster
{
    public class NGVodPosterDataController
    {
        private static readonly string _posterSourceMapTbl = "[dbo].[tFIOSVODAssetPosterSourceMap]";

        /// <summary>
        /// Bulk insert Vod Asset Poster Source Map to SQL database
        /// </summary>
        /// <param name="VodAssets">All VOD Assets</param>
        /// <param name="ConnectionString">SQL Connection String</param>
        public static void BulkInsertData (VODAsset[] VodAssets, string ConnectionString)
        {
            VodAssets = VodAssets.Where(x => !string.IsNullOrEmpty(x.PosterSource)).ToArray();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "TRUNCATE TABLE " + _posterSourceMapTbl;
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                }
                using (var bcp = new SqlBulkCopy(connection))
                {
                    bcp.DestinationTableName = _posterSourceMapTbl;
                    bcp.ColumnMappings.Add("AssetId", "strAssetId");
                    bcp.ColumnMappings.Add("PosterSource", "strPosterFile");

                    using (var bcpReader = ObjectReader.Create(VodAssets, "AssetId", "PosterSource"))
                    {
                        bcp.WriteToServer(bcpReader);
                    }
                }
            }
        }

        /// <summary>
        /// Inserts or Updates a single asset poster source into the database async
        /// </summary>
        /// <param name="Asset">Vod Asset to insert or update the poster source value</param>
        /// <param name="ConnectionString">SQL Connection String</param>
        /// <returns></returns>
        public static async Task InsertAssetAsync (VODAsset Asset, string ConnectionString)
        {
            if (string.IsNullOrEmpty(Asset.PosterSource))
            {
                throw new ArgumentNullException("Asset poster source cannot be null");
            }

            StringBuilder strCmd = new StringBuilder();
            strCmd.AppendFormat("SELECT TOP 1 * FROM {0} a WHERE a.strAssetId = {1}", _posterSourceMapTbl ,Asset.AssetId);
            bool isAlreadyExists = false;

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = connection.CreateCommand())
            {
                await connection.OpenAsync();

                command.CommandText = strCmd.ToString();
                command.CommandType = CommandType.Text;
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    isAlreadyExists = reader.HasRows;
                }

                strCmd.Clear();
                if (isAlreadyExists)
                {
                    strCmd.AppendFormat("UPDATE {0} SET {0}.strPosterFile = '{1}' WHERE {0}.strAssetId = '{2}'", _posterSourceMapTbl, Asset.PosterSource, Asset.AssetId);
                }
                else
                {
                    strCmd.AppendFormat("INSERT INTO {0} VALUES ('{1}', '{2}')", _posterSourceMapTbl, Asset.AssetId, Asset.PosterSource);
                }

                command.Transaction = connection.BeginTransaction();
                command.CommandText = strCmd.ToString();

                try
                {
                    await command.ExecuteNonQueryAsync();
                    command.Transaction.Commit();
                }
                catch
                {
                    command.Transaction.Rollback();
                    throw;
                }
            }
        }
    }
}
