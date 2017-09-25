using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Dapper;
using Dapper.Contrib.Extensions;
using Dapper.Mapper;
using ChannelAPI.Models;

namespace ChannelAPI.Repositories
{
    public class RegionRepository
    {
        private string _version;
        private ILogger _logger;
        private IEnumerable<string> _vhos;
        private const string TABLENAME = "tFIOSRegion";

        public RegionRepository(IConfiguration config, ILoggerFactory loggerFactory)
        {
            this._version = config.GetValue<string>("FiosChannelData:VersionAliasId");
            this._logger = loggerFactory.CreateLogger<StationRepository>();
            var vhoSection = config.GetSection("FiosChannelData:ActiveVHOs");
            this._vhos = vhoSection.GetChildren().AsList().Select(x => x.Value);
        }

        public async Task<IEnumerable<FiosRegion>> GetAllAsync()
        {
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.GetAllAsync<FiosRegion>();
            }
        }

        public async Task<IEnumerable<string>> GetActiveVHOs()
        {
            var query = string.Format("SELECT DISTINCT strVHOId FROM {0} WHERE strVHOId IN ({1})", TABLENAME, string.Join(',', this._vhos.Select(x => '\'' + x + '\'')));
            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.QueryAsync<string>(query);
            }
        }

        public async Task<IEnumerable<string>> GetActiveRegions()
        {
            var query = string.Format("SELECT DISTINCT strFIOSRegionName FROM {0} WHERE strVHOId IN ({1})", TABLENAME, string.Join(',', this._vhos.Select(x => '\'' + x + '\'')));

            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.QueryAsync<string>(query, new { vhos = string.Join(',', this._vhos) });
            }
        }
    }
}
