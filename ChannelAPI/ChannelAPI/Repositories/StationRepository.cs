using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Dapper.Mapper;
using ChannelAPI.Models;

namespace ChannelAPI.Repositories
{

    public class StationRepository : IRepository<StationDTO>
    {
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
    }
}
