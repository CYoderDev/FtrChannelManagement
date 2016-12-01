using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastMember;
using FrontierVOps.Data;

namespace FrontierVOps.FiOS.NGVODPoster
{
    public class NGVodPosterDataController : IDisposable
    {
        /// <summary>
        /// Name of the table that contains maps to the poster source file name
        /// </summary>
        private static readonly string _posterSourceMapTbl = "[dbo].[tFIOSVODAssetPosterSourceMap]";

        /// <summary>
        /// Connection is open or closed
        /// </summary>
        private bool isOpen = false;

        /// <summary>
        /// SQL connection to the database
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// SQL transaction
        /// </summary>
        private SqlTransaction transaction;

        /// <summary>
        /// Determines whether there is an existing commit taking place on the transaction
        /// </summary>
        private bool committing = false;

        /// <summary>
        /// Determines whether the connection is currently resetting in order to open a second transaction
        /// </summary>
        private bool resetting = false;

        /// <summary>
        /// Determines whether there is currently an action taking place on the transaction that would
        /// prevent the transaction from being able to perform a commit
        /// </summary>
        private int inserting = 0;

        /// <summary>
        /// The number of successful inserts that has taken place on the current transaction
        /// </summary>
        int inserts = 0;

        public NGVodPosterDataController(string ConnectionString)
        {
            connection = new SqlConnection(ConnectionString);
        }

        public void Open()
        {
            if (!this.isOpen)
            {
                connection.Open();
                this.isOpen = true;
            }
        }

        public async Task OpenAsync(CancellationToken cancelToken)
        {
            if (!this.isOpen)
            {
                await connection.OpenAsync(cancelToken);
                this.isOpen = true;
            }
        }

        /// <summary>
        /// Creates a transaction on the connection
        /// </summary>
        public void BeginTransaction()
        {
            if (!this.isOpen)
                this.Open();

            this.transaction = this.connection.BeginTransaction();
            committing = false;
        }

        /// <summary>
        /// Performs a commit on the transaction and syncs all parallel instances
        /// </summary>
        /// <param name="resetConnection">Whether the connection should also be reset in order to create a new transaction</param>
        /// <returns></returns>
        public bool CommitTransaction(bool resetConnection = false)
        {
            if (this.transaction != null && this.isOpen && !this.committing && !this.resetting)
            {
                int retry = 0;
                this.committing = true;
                try
                {
                    while (retry != 5)
                    {
                        try
                        {
                            this.transaction.Commit();
                            this.committing = false;
                            if (resetConnection)
                                this.ResetConnection();
                            break;
                        }
                        catch
                        {
                            retry++;
                            Thread.Sleep(100);
                        }
                    }
                }
                finally
                {
                    this.committing = false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets the connection in order to be able to create a new transaction
        /// </summary>
        public void ResetConnection()
        {
            if (!this.committing & this.isOpen)
            {
                this.resetting = true;
                this.isOpen = false;
                string connectString = this.connection.ConnectionString;
                while (this.inserting != 0)
                {
                    Thread.Sleep(100);
                }
                this.connection.Dispose();
                this.transaction.Dispose();

                this.connection = new SqlConnection(connectString);
                this.Open();
                this.transaction = this.connection.BeginTransaction();
                this.inserts = 0;
                this.resetting = false;
            }
        }

        /// <summary>
        /// Roles back the existing transaction
        /// </summary>
        public void RollbackTransaction()
        {
            if (this.transaction != null && this.isOpen && this.inserting == 0)
                this.transaction.Rollback();
        }

        /// <summary>
        /// Inserts or Updates a single asset poster source into the database async
        /// </summary>
        /// <param name="Asset">Vod Asset to insert or update the poster source value</param>
        /// <param name="ConnectionString">SQL Connection String</param>
        /// <returns></returns>
        public async Task InsertAssetAsync (VODAsset Asset, CancellationToken CancelToken)
        {
            if (string.IsNullOrEmpty(Asset.PosterSource))
            {
                throw new ArgumentNullException("Asset poster source cannot be null");
            }

            

            while (this.committing || this.resetting)
            {
                await Task.Delay(100, CancelToken);
            }

            if (!this.isOpen)
                await this.OpenAsync(CancelToken);

            StringBuilder strCmd = new StringBuilder();
            strCmd.AppendFormat("SELECT TOP 1 * FROM {0} a WHERE a.strAssetId = {1}", _posterSourceMapTbl ,Asset.AssetId);
            bool isAlreadyExists = false;

            CancelToken.ThrowIfCancellationRequested();
            this.inserting++;
            try
            {
                using (var command = this.connection.CreateCommand())
                {
                    command.Transaction = this.transaction;

                    command.CommandText = strCmd.ToString();
                    command.CommandType = CommandType.Text;
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        isAlreadyExists = reader.HasRows;
                    }

                    strCmd.Clear();
                    if (isAlreadyExists)
                    {
                        strCmd.AppendFormat("UPDATE {0} SET {0}.strPosterFile = '{1}' WHERE {0}.strAssetId = '{2}'", _posterSourceMapTbl, Path.GetFileName(Asset.PosterSource), Asset.AssetId);
                    }
                    else
                    {
                        strCmd.AppendFormat("INSERT INTO {0} VALUES ('{1}', '{2}')", _posterSourceMapTbl, Asset.AssetId, Path.GetFileName(Asset.PosterSource));
                    }

                    command.CommandText = strCmd.ToString();

                    await command.ExecuteNonQueryAsync(CancelToken);
                }
            }
            finally
            {
                this.inserting--;
            }
            this.inserts++;

            if (this.inserts % 100 == 0)
            {
                this.CommitTransaction(true);
            }
        }

        /// <summary>
        /// Gets all Vod Asset Id to Poster Maps for an individual VHO
        /// </summary>
        /// <param name="ConnectionString">SQL Connection String</param>
        /// <param name="SrcDir">Source directory where the raw poster files are stored</param>
        /// <returns>Enumerable of tuple values with asset id and poster file</returns>
        public async Task<Tuple<int, string>[]> GetAllPosterSourceMapsByVhoAsync(string SrcDir, CancellationToken CancelToken)
        {
            StringBuilder sbCmd = new StringBuilder();
            sbCmd.AppendFormat("SELECT * FROM {0}", _posterSourceMapTbl);

            if (!this.isOpen)
                await connection.OpenAsync();

            List<Tuple<int, string>> retList = new List<Tuple<int, string>>();

            CancelToken.ThrowIfCancellationRequested();
            using (var command = this.connection.CreateCommand())
            {
                command.CommandText = sbCmd.ToString();
                command.CommandType = CommandType.Text;

                using (var reader = await command.ExecuteReaderAsync(CancelToken))
                {
                    while (await reader.ReadAsync(CancelToken))
                    {
                        CancelToken.ThrowIfCancellationRequested();
                        //yield return new Tuple<int, string>(int.Parse(reader.GetString(0)), Path.Combine(SrcDir, reader.GetString(1)));
                        retList.Add(new Tuple<int, string>(int.Parse(reader.GetString(0)), Path.Combine(SrcDir, reader.GetString(1))));
                    }
                }
            }

            return retList.ToArray();
        }

        /// <summary>
        /// Gets all VOD Asset Id to Poster Maps for all VHO's
        /// </summary>
        /// <param name="config">Configuration that contains all VHO data</param>
        /// <returns>Enumerable with a tuple that contains the vod asset id and poster file (full path)</returns>
        public static IEnumerable<Tuple<int, string>> GetAllPosterSourceMaps(NGVodPosterConfig config, CancellationToken CancelToken)
        {
            StringBuilder sbCmd = new StringBuilder();
            sbCmd.AppendFormat("SELECT * FROM {0} WHERE {0}.strPosterFile IS NOT NULL", _posterSourceMapTbl);

            foreach (var vho in config.Vhos.Keys)
            {
                CancelToken.ThrowIfCancellationRequested();
                NGVodVHO vodvho = config.Vhos[vho];

                foreach (var dr in DBFactory.SQL_ExecuteReader(vodvho.IMGDs.CreateConnectionString(vodvho.IMGDb), sbCmd.ToString(), CommandType.Text, null))
                {
                    CancelToken.ThrowIfCancellationRequested();
                    yield return new Tuple<int, string>(int.Parse(dr.GetString(0)), dr.IsDBNull(1) ? string.Empty : Path.Combine(config.SourceDir, dr.GetString(1)));
                }
            }
        }

        /// <summary>
        /// Deletes a poster source map record from the database for a VOD Asset
        /// </summary>
        /// <param name="ConnectionString">SQL Connection String</param>
        /// <param name="VAsset">Vod Asset to have it's poster source map deleted</param>
        /// <returns></returns>
        public async Task DeleteVodAssetAsync(VODAsset VAsset, CancellationToken CancelToken)
        {
            StringBuilder sbCmd = new StringBuilder();
            sbCmd.AppendFormat("SELECT TOP 1 * FROM {0} WHERE {0}.strAssetId = '{1}'", _posterSourceMapTbl, VAsset.AssetId);

            bool isExists = false;

            CancelToken.ThrowIfCancellationRequested();

            while (this.committing || this.resetting)
            {
                await Task.Delay(100, CancelToken);
            }

            this.inserting++;
            try
            {
                using (var command = this.connection.CreateCommand())
                {


                    await this.OpenAsync(CancelToken);


                    command.CommandText = sbCmd.ToString();


                    using (var reader = await command.ExecuteReaderAsync(CancelToken))
                    {
                        isExists = reader.HasRows;
                    }

                    if (!isExists)
                        return;

                    sbCmd.Clear();
                    sbCmd.AppendFormat("DELETE FROM {0} WHERE {0}.strAssetId = '{1}'", _posterSourceMapTbl, VAsset.AssetId);

                    command.Transaction = this.transaction;
                    command.CommandText = sbCmd.ToString();

                    if (!CancelToken.IsCancellationRequested)
                        await command.ExecuteNonQueryAsync(CancelToken);
                }
            }
            finally
            {
                this.inserting--;
            }
        }

        /// <summary>
        /// Deletes records in the source map SQL table that are not currently active assets
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task CleanupSourceMapTable(CancellationToken cancelToken)
        {
            if (!this.isOpen)
                await this.OpenAsync(cancelToken);

            cancelToken.ThrowIfCancellationRequested();
            using (var command = this.connection.CreateCommand())
            {
                command.CommandText = "dbo.sp_FUI_CleanVodAssetPosterSourceMap";
                command.CommandType = CommandType.StoredProcedure;

                using (var trans = this.connection.BeginTransaction())
                {
                    command.Transaction = trans;

                    try
                    {
                        if (!cancelToken.IsCancellationRequested)
                        {
                            await command.ExecuteNonQueryAsync(cancelToken);
                            command.Transaction.Commit();
                        }
                    }
                    catch
                    {
                        if (!cancelToken.IsCancellationRequested)
                            command.Transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.transaction != null)
                    this.transaction.Dispose();

                if (this.isOpen)
                    this.connection.Close();

                this.connection.Dispose();
            }
        }

        ~NGVodPosterDataController()
        {
            Dispose(false);
        }
        #endregion Dispose
    }
}
