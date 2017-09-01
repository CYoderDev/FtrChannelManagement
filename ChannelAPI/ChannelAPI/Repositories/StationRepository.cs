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

    public class StationRepository : IRepository<StationDTO>
    {
        private string _version;

        public StationRepository(IConfiguration config)
        {
            this._version = config.GetValue<string>("FiosChannelData:VersionAliasId");
        }

        public long Add(StationDTO obj)
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                return connection.Insert<StationDTO>(obj);
            }
        }

        public async Task<long> AddAsync(StationDTO obj)
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.InsertAsync<StationDTO>(obj);
            }
        }

        public StationDTO FindByID(string id)
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                return connection.Get<StationDTO>(id);
            }
        }

        public async Task<StationDTO> FindByIDAsync(string id)
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.GetAsync<StationDTO>(id);
            }
        }

        public IEnumerable<StationDTO> GetAll()
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                return connection.GetAll<StationDTO>();
            }
        }

        public async Task<IEnumerable<StationDTO>> GetAllAsync()
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.GetAllAsync<StationDTO>();
            }
        }

        public void Remove(StationDTO obj)
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                connection.Delete<StationDTO>(obj);
            }
        }

        public async void RemoveAsync(StationDTO obj)
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                await connection.DeleteAsync<StationDTO>(obj);
            }
        }

        public void Update(StationDTO obj)
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                connection.Update<StationDTO>(obj);
            }
        }

        public async void UpdateAsync(StationDTO obj)
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                await connection.UpdateAsync<StationDTO>(obj);
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
