using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using FrontierVOps.Data;
using FrontierVOps.ChannelMapWS.Controllers;
using FrontierVOps.ChannelMapWS.Models;

namespace FrontierVOps.ChannelMapWS.Services
{
    public class ChannelService
    {
        #region GET
        #region Internal
        internal async Task<Channel> getByServiceId(int serviceId, string version = "1.9")
        {
            string command = string.Format("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '{0}' AND strFIOSServiceId = '{1}'", version, serviceId);
            return (await GetAsync(command)).FirstOrDefault();
        }

        internal async Task<IEnumerable<Channel>> getByVHO(int vhoId, string version = "1.9")
        {
            string command = string.Format("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '{0}' AND strVHOId = 'VHO{1}'", version, vhoId);
            return await GetAsync(command);
        }

        internal async Task<IEnumerable<Channel>> getByRegion(int regionId, string version = "1.9")
        {
            string command = string.Format("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '{0}' AND strFIOSRegionId = '{1}'", version, regionId);
            return await GetAsync(command);
        }

        internal async Task<IEnumerable<Channel>> getByLogo(int bitmapId, string version = "1.9")
        {
            string command = string.Format("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '{0}' AND intBitmapId = {1}", version, bitmapId);
            return await GetAsync(command);
        }

        internal async Task<IEnumerable<Channel>> getByPosition(int position, string version = "1.9")
        {
            string command = string.Format("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '{0}' AND intChannelPosition = {1}", version, position);
            return await GetAsync(command);
        }

        internal async Task<IEnumerable<Channel>> getAll(int? serviceId = null, int? position = null, int? logoId = null, int? vhoId = null, int? regionId = null, string stationName = "", string callSign = "", string regionName = "", string version = "1.9", bool? isVirtual = null)
        {
            StringBuilder command = new StringBuilder();
            command.Append("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "'");

            if (null != serviceId)
                command.AppendFormat(" AND strFIOSServiceId = '{0}'", serviceId);
            if (null != position)
                command.AppendFormat(" AND intChannelPosition = {0}", position);
            if (null != logoId)
                command.AppendFormat(" AND intBitmapId = {0}", logoId);
            if (null != vhoId)
                command.AppendFormat(" AND strVHOId = 'VHO{0}'", vhoId);
            if (null != regionId)
                command.AppendFormat(" AND strFIOSRegionId = '{0}'", regionId);
            if (!string.IsNullOrEmpty(stationName))
                command.AppendFormat(" AND strStationName LIKE '{0}'", stationName);
            if (!string.IsNullOrEmpty(callSign))
                command.AppendFormat(" AND strStationCallSign LIKE '{0}'", callSign);
            if (!string.IsNullOrEmpty(regionName))
                command.AppendFormat(" AND strFIOSRegionName LIKE '{0}'", regionName);
            if (isVirtual.HasValue)
                command.Append(" AND bitIsVirtual = 1");

            return await GetAsync(command.ToString());
        }

        internal async Task<IEnumerable<Channel>> getAllVirtual(int? vhoId = null, string version = "1.9")
        {
            return await getAll(null, null, null, vhoId, null, null, null, null, version, true);
        }

        internal async Task<IEnumerable<Channel>> getNoLogo(int? vhoId = null, int? regionId = null, string regionName = null, string version = "1.9")
        {
            var inactiveLogos = await getChannelWithInactiveLogo(vhoId, regionId, regionName, version);
            var defaultLogos = await getAll(null, null, 10000, vhoId, regionId, null, null, regionName, version, null);
            return defaultLogos.Concat(inactiveLogos).OrderBy(x => x.ChannelNumber);
        }
        #endregion Internal

        #region Private
        private async Task<IEnumerable<Channel>> getChannelWithInactiveLogo(int? vhoId = null, int? regionId = null, string regionName = null, string version = "1.9")
        {
            StringBuilder command = new StringBuilder();
            StringBuilder conditions = new StringBuilder();
            command.AppendFormat("SELECT intBitmapId FROM vChannelMap WHERE strFIOSVersionAliasId = '{0}'", version);

            if (null != vhoId)
                conditions.AppendFormat(" AND strVHOId = 'VHO{0}'", vhoId);
            if (null != regionId)
                conditions.AppendFormat(" AND strFIOSRegionId = '{0}'", regionId);
            if (!string.IsNullOrEmpty(regionName))
                conditions.AppendFormat(" AND strFIOSRegionName LIKE '{0}'", regionName);

            command.Append(conditions.ToString());

            var clCtrl = new ChannelLogoController();

            var missingIdsFromDir = await clCtrl.getMissingBitmapIds();
            List<int> existingIdsFromDB = new List<int>();

            existingIdsFromDB.AddRange((await GetAsync(command.ToString())).Select(x => x.Logo.BitmapId));

            //await DBFactory.SQL_ExecuteReaderAsync(WebApiConfig.ConnectionString, command.ToString(), System.Data.CommandType.Text, null, dr =>
            //{
            //    while (dr.Read())
            //    {
            //        existingIdsFromDB.Add(dr.GetInt32(0));
            //    }
            //});

            List<int> assignedMissingIds = existingIdsFromDB.Intersect(missingIdsFromDir).ToList();

            var strJoined = String.Join(",", assignedMissingIds);
            command.Clear();
            command.AppendFormat("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '{0}' AND intBitmapId IN ({1})", version, strJoined);
            command.Append(conditions.ToString());

            return await GetAsync(command.ToString());
        }

        private async Task<IEnumerable<Channel>> GetAsync(string command)
        {
            List<Channel> channels = new List<Channel>();

            await DBFactory.SQL_ExecuteReaderAsync(WebApiConfig.ConnectionString, command, System.Data.CommandType.Text, null, dr =>
            {
                var clCtrl = new ChannelLogoController();
                while (dr.Read())
                {
                    var chan = new Channel();

                    chan.ServiceID = int.Parse(dr.GetString(0));
                    chan.ChannelNumber = dr.GetInt32(1);
                    chan.CallSign = dr.GetString(2);
                    chan.StationName = dr.GetString(3);
                    chan.Logo = clCtrl.getLogoInfo(dr.GetInt32(4));
                    chan.Version = dr.IsDBNull(5) ? null : dr.GetString(5);
                    chan.VHO = dr.IsDBNull(6) ? null : dr.GetString(6);
                    chan.LastUpdate = dr.IsDBNull(7) ? (DateTime?)null : dr.GetDateTime(7);
                    chan.RegionID = int.Parse(dr.GetString(8));
                    chan.RegionName = dr.IsDBNull(9) ? null : dr.GetString(9);
                    chan.StationDescription = dr.IsDBNull(10) ? null : dr.GetString(10);
                    chan.UniqueString = dr.IsDBNull(11) ? null : dr.GetString(11);
                    chan.IsVirtual = dr.GetBoolean(12);

                    channels.Add(chan);
                }
            });

            return channels;
        }
        #endregion Private
        #endregion GET

        #region PUT
        internal async Task<int> updateStation(Tuple<string, object>[] parameters)
        {
            string sproc = "sp_FUIUpdateStation";
            return 0;
        }
        #endregion PUT
    }
}