using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Dapper.Contrib.Extensions;
using Dapper.Mapper;
using ChannelAPI.Models;

namespace ChannelAPI.Repositories
{
    public class ChannelRepository : IRepository<ChannelDTO>
    {
        private string _version { get; set; }
        public ChannelRepository(IConfiguration config)
        {
            this._version = config.GetValue<string>("FiosChannelData:VersionAliasId");
            if (string.IsNullOrEmpty(_version))
            {
                throw new KeyNotFoundException("Could not find FiosChannelData:VersionAliasId value in configuration file.");
            }
        }

        public long Add(ChannelDTO obj)
        {
            throw new NotImplementedException();
        }

        public async Task<long> AddAsync(ChannelDTO obj)
        {
            throw new NotImplementedException();
        }

        public ChannelDTO FindByID(string id)
        {
            using (var connection = DapperFactory.GetOpenConnection())
            {
                return connection.Get<ChannelDTO>(id);
            }
        }

        public async Task<ChannelDTO> FindByIDAsync(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ChannelDTO>> FindAllByIDAsync(string id)
        {
            var query = getQuery(this._version);
            query.AppendFormat("AND a.strFIOSServiceId = {0}", id);
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.QueryAsync<ChannelDTO>(query.ToString());
            }
        }

        public IEnumerable<ChannelDTO> GetAll()
        {
            var query = getQuery(this._version).Replace("*", "a.strFIOSServiceId");
            using (var connection = DapperFactory.GetOpenConnection())
            {
                return connection.Query<ChannelDTO>(query.ToString());
            }
        }

        public async Task<IEnumerable<ChannelDTO>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<string>> GetAllIdsAsync()
        {
            var query = getQuery(this._version).Replace("*", "a.strFIOSServiceId");
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.QueryAsync<string>(query.ToString());
            }
        }

        public async Task<IEnumerable<ChannelDTO>> GetByRegionAsync(string id)
        {
            var query = getQuery(this._version);
            query.AppendFormat("AND a.strFIOSRegionId = '{0}'", id);
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.QueryAsync<ChannelDTO>(query.ToString());
            }
        }

        public async Task<IEnumerable<ChannelDTO>> GetByVHOId(string id)
        {
            var query = getQuery(this._version);
            query.AppendFormat("AND a.strVHOId = 'VHO{0}'", id);
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.QueryAsync<ChannelDTO>(query.ToString());
            }
        }

        public async Task<IEnumerable<ChannelDTO>> GetLikeColumn(string value, string columnName)
        {
            var query = getQuery(this._version);
            query.AppendFormat("AND a.{0} LIKE '%{1}%'", columnName, value);
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.QueryAsync<ChannelDTO>(query.ToString());
            }
        }

        public async Task<IEnumerable<ChannelDTO>> GetByStationName(string name)
        {
            var query = getQuery(this._version);
            query.AppendFormat("AND a.strStationName LIKE '%{0}%'", name);
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.QueryAsync<ChannelDTO>(query.ToString());
            }
        }

        public async Task<IEnumerable<ChannelDTO>> GetByCallSign(string name)
        {
            var query = getQuery(this._version);
            query.AppendFormat("AND a.strStationCallSign LIKE '%{0}%'", name);
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.QueryAsync<ChannelDTO>(query.ToString());
            }
        }

        public async Task<FiosStationGenre> GetByGenreId(int id)
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.GetAsync<FiosStationGenre>(id);
            }
        }

        public void Remove(ChannelDTO obj)
        {
            throw new NotImplementedException();
        }

        public async Task RemoveAsync(ChannelDTO obj)
        {
            throw new NotImplementedException();
        }

        public void Update(ChannelDTO obj)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateAsync(ChannelDTO obj)
        {
            throw new NotImplementedException();
        }

        private StringBuilder getQuery(string version)
        {
            var query = new StringBuilder();
            query.AppendFormat("SELECT DISTINCT * FROM vChannels a WHERE a.strFIOSVersionAliasId = '{0}' ", version);
            return query;
        }
    }
}
