using System;
using System.Collections.Generic;
//using System.Data;
//using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Dapper.Contrib.Extensions;
using Dapper.Mapper;
using ChannelAPI.Models;

namespace ChannelAPI.Repositories
{

    public class StationRepository : IRepository<FiosStation>
    {
        private string _version;

        public StationRepository(IConfiguration config)
        {
            this._version = config.GetValue<string>("FiosChannelData:VersionAliasId");
        }

        public long Add(FiosStation obj)
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                return connection.Insert<FiosStation>(obj);
            }
        }

        public async Task<long> AddAsync(FiosStation obj)
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.InsertAsync<FiosStation>(obj);
            }
        }

        public FiosStation FindByID(string id)
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                return connection.Get<FiosStation>(id);
            }
        }

        public async Task<FiosStation> FindByIDAsync(string id)
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.GetAsync<FiosStation>(id);
            }
        }

        public IEnumerable<FiosStation> GetAll()
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                return connection.GetAll<FiosStation>();
            }
        }

        public async Task<IEnumerable<FiosStation>> GetAllAsync()
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.GetAllAsync<FiosStation>();
            }
        }

        public void Remove(FiosStation obj)
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                connection.Delete<FiosStation>(obj);
            }
        }

        public async Task RemoveAsync(FiosStation obj)
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                await connection.DeleteAsync<FiosStation>(obj);
            }
        }

        public void Update(FiosStation obj)
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                connection.Update<FiosStation>(obj);
            }
        }

        public async Task UpdateAsync(FiosStation obj)
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                await connection.UpdateAsync<FiosStation>(obj);
            }
        }

        public async Task<int> UpdateBitmap(string FiosServiceId, int newBitmapId)
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                string query = "SELECT * FROM tFIOSBitmapStationMap WHERE strFIOSServiceId = @id AND strFIOSVersionAliasId = @version";
                var bmDTO = await connection.QueryFirstOrDefaultAsync<BitmapStationMapDTO>(query, new { id = FiosServiceId, version = this._version });
                if (bmDTO == null || bmDTO.intBitmapId == 0)
                {
                    bmDTO = new BitmapStationMapDTO();
                    bmDTO.intBitmapId = newBitmapId;
                    bmDTO.strFIOSServiceId = FiosServiceId;
                    bmDTO.strFIOSVersionAliasId = this._version;
                    bmDTO.dtCreateDate = DateTime.Now;
                    bmDTO.dtLastUpdateDate = DateTime.Now;
                    return await connection.InsertAsync<BitmapStationMapDTO>(bmDTO);
                }
                else
                {
                    query = @"
                             UPDATE tFIOSBitmapStationMap SET dtCreateDate = @date, dtLastUpdateDate = @date, intBitmapId = @bmid 
                             WHERE strFIOSServiceId = @id";
                    return await connection.ExecuteAsync(query, new { date = DateTime.Now, bmid = newBitmapId, id = FiosServiceId });
                }
            }
        }
    }
}
