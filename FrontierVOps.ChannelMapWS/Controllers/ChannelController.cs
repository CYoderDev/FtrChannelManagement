using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web;
using FrontierVOps.Common.Web.Security;
using FrontierVOps.Data;
using FrontierVOps.ChannelMapWS.Models;

namespace FrontierVOps.ChannelMapWS.Controllers
{
    public class ChannelController : ApiController
    {
        #region GET
        [HttpGet]
        [Route("api/FiOS/Channel/ServiceId/{serviceId:int}")]
        public async Task<IHttpActionResult> GetByServiceId(int serviceId, double version = 1.9)
        {
            try
            {
                string command = "SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "' AND strFIOSServiceId = '" + serviceId.ToString() + "'";
                return Ok<IEnumerable<Channel>>(await GetAsync(command));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/FiOS/Channel/VHO/{vhoId:int:min(1)}")]
        public async Task<IHttpActionResult> GetByVHO(int vhoId, double version = 1.9)
        {
            try
            {
                string command = "SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "' AND strVHOId = 'VHO" + vhoId.ToString() + "'";
                return Ok<IEnumerable<Channel>>(await GetAsync(command));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/FiOS/Channel/Region/{regionId:int}")]
        public async Task<IHttpActionResult> GetbyRegion(int regionId, double version = 1.9)
        {
            try
            {
                string command = "SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "' AND strFIOSRegionId = '" + regionId.ToString() + "'";
                return Ok<IEnumerable<Channel>>(await GetAsync(command));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/FiOS/Channel/Logo/{logoId:int}")]
        public async Task<IHttpActionResult> GetbyLogo(int logoId, double version = 1.9)
        {
            try
            {
                string command = "SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "' AND intBitmapId = " + logoId;
                return Ok<IEnumerable<Channel>>(await GetAsync(command));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/FiOS/Channel/{position:int:min(1)}")]
        public async Task<IHttpActionResult> GetbyPosition(int position, double version = 1.9)
        {
            try
            {
                string command = "SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "' AND intChannelPosition = " + position;
                return Ok<IEnumerable<Channel>>(await GetAsync(command));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/FiOS/Channel/query")]
        public async Task<IHttpActionResult> GetAllChannels(int? serviceId = null, int? position = null, int? logoId = null, int? vhoId = null, int? regionId = null, string stationName = "", string callSign = "", string regionName = "", double version = 1.9)
        {
            try
            {
                StringBuilder command = new StringBuilder();
                command.Append("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "'");

                if (null != serviceId)
                    command.Append(" AND strFIOSServiceId = '" + serviceId.ToString() + "'");
                if (null != position)
                    command.Append(" AND intChannelPosition = " + position);
                if (null != logoId)
                    command.Append(" AND intBitmapId = " + logoId);
                if (null != vhoId)
                    command.Append(" AND strVHOId = 'VHO" + vhoId.ToString() + "'");
                if (null != regionId)
                    command.Append(" AND strFIOSRegionId = '" + regionId.ToString() + "'");
                if (!string.IsNullOrEmpty(stationName))
                    command.Append(" AND strStationName LIKE '" + stationName + "'");
                if (!string.IsNullOrEmpty(callSign))
                    command.Append(" AND strStationCallSign LIKE '" + callSign + "'");
                if (!string.IsNullOrEmpty(regionName))
                    command.Append(" AND strFIOSRegionName LIKE '" + regionName + "'");

                var result = await GetAsync(command.ToString());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/FiOS/Channel/Virtual")]
        public async Task<IHttpActionResult> GetVirtualChannels(int? vhoId = null, string version = "1.9")
        {
            try
            {
                StringBuilder command = new StringBuilder();
                command.AppendFormat("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '{0}' AND bitIsVirtual = 1", version);

                if (vhoId.HasValue)
                    command.AppendFormat(" AND strVHOId = 'VHO{0}'", vhoId);

                var result = await GetAsync(command.ToString());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/FiOS/Channel/Logo/Missing")]
        public async Task<IHttpActionResult> GetNoLogo(int? vhoId = null, int? regionId = null, string regionName = "", double version = 1.9)
        {
            try
            {
                StringBuilder command = new StringBuilder();
                command.Append("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version + "' AND intBitmapId = " + 10000);

                if (null != vhoId)
                    command.Append(" AND strVHOId = 'VHO" + vhoId + "'");
                if (null != regionId)
                    command.Append(" AND strFIOSRegionId = '" + regionId + "'");
                if (!string.IsNullOrEmpty(regionName))
                    command.Append(" AND strFIOSRegionName LIKE '" + regionName + "'");

                var inactiveLogos = await getChannelWithInactiveLogo(version, vhoId, regionId, regionName);
                var result = await GetAsync(command.ToString());

                return Ok(result.Concat(inactiveLogos));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private IEnumerable<Channel> Get(string command)
        {
            foreach (var record in DBFactory.SQL_ExecuteReader(WebApiConfig.ConnectionString, command, System.Data.CommandType.Text))
            {
                var chan = new Channel();
                var clCtrl = new ChannelLogoController();

                chan.ServiceID = int.Parse(record.GetString(0));
                chan.ChannelNumber = record.GetInt32(1);
                chan.CallSign = record.GetString(2);
                chan.StationName = record.GetString(3);
                chan.Logo = clCtrl.getLogoInfoAsync(record.GetInt32(4)).Result ?? clCtrl.getLogoInfoAsync(record.GetInt32(4), false).Result;
                chan.Version = record.GetString(5);
                chan.VHO = record.GetString(6);
                chan.LastUpdate = record.IsDBNull(7) ? (DateTime?)null : record.GetDateTime(7);
                chan.RegionID = int.Parse(record.GetString(8));
                chan.RegionName = record.IsDBNull(9) ? null : record.GetString(9);
                chan.StationDescription = record.IsDBNull(10) ? null : record.GetString(10);
                chan.UniqueString = record.IsDBNull(11) ? null : record.GetString(11);

                yield return chan;
            }
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

        private async Task<IEnumerable<Channel>> getChannelWithInactiveLogo(double version = 1.9, int? vhoId = null, int? regionId = null, string regionName = "")
        {
            StringBuilder command = new StringBuilder();
            StringBuilder conditions = new StringBuilder();
            command.AppendFormat("SELECT intBitmapId FROM vChannelMap WHERE strFIOSVersionAliasId = '{0}'", version);

            if (null != vhoId)
                conditions.AppendFormat(" AND strVHOId = 'VHO{0}'",vhoId);
            if (null != regionId)
                conditions.AppendFormat(" AND strFIOSRegionId = '{0}'", regionId);
            if (!string.IsNullOrEmpty(regionName))
                conditions.AppendFormat(" AND strFIOSRegionName LIKE '{0}'", regionName);

            command.Append(conditions.ToString());

            var clCtrl = new ChannelLogoController();

            var missingIdsFromDir = await clCtrl.getMissingBitmapIds();
            List<int> existingIdsFromDB = new List<int>();

            await DBFactory.SQL_ExecuteReaderAsync(WebApiConfig.ConnectionString, command.ToString(), System.Data.CommandType.Text, null, dr =>
            {
                while (dr.Read())
                {
                    existingIdsFromDB.Add(dr.GetInt32(0));
                }
            });

            List<int> assignedMissingIds = existingIdsFromDB.Intersect(missingIdsFromDir).ToList();

            var strJoined = String.Join(",", assignedMissingIds);
            command.Clear();
            command.AppendFormat("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '{0}' AND intBitmapId IN ({1})", version, strJoined);
            command.Append(conditions.ToString());

            return await GetAsync(command.ToString());
        }
        #endregion GET

        #region PUT
        [HttpPut]
        [ValidateAntiForgery]
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("api/Channel/Update/CallSign/[serviceId]")]
        public async Task<IHttpActionResult> UpdateCallSign(int serviceId, string callSign)
        {
            try
            {
                string sproc = "sp_FUIUpdateStation";

                Tuple<string, object>[] parameters = new Tuple<string, object>[2];

                parameters[0] = new Tuple<string, object>("strServiceId", serviceId.ToString());
                parameters[1] = new Tuple<string, object>("strCallSign", callSign);

                int rows = await DBFactory.SQL_ExecuteNonQueryAsync(WebApiConfig.ConnectionString, sproc, System.Data.CommandType.StoredProcedure, parameters);

                return Ok(rows > 0);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("api/Channel/Update/Station/[serviceId]")]
        public async Task<IHttpActionResult> UpdateStation(int serviceId, string name = null, string callSign = null, string description = null, string about = null)
        {
            try
            {
                string sproc = "sp_FUIUpdateStation";

                string unique = null;

                var oldChannelResult = await GetByServiceId(serviceId);
                var oldChannelContent = await oldChannelResult.ExecuteAsync(CancellationToken.None);
                var oldChannel = await oldChannelContent.Content.ReadAsAsync<IEnumerable<Channel>>();

                if (!(string.IsNullOrEmpty(name) && string.IsNullOrEmpty(callSign)))
                {

                    unique = name ?? oldChannel.FirstOrDefault().StationName;
                    unique += callSign ?? oldChannel.FirstOrDefault().CallSign;
                    unique += 300; //Change this with correct feed timezone information
                    unique = unique.Replace(" ", "").Trim();
                }

                Tuple<string, object>[] parameters = new Tuple<string, object>[6];

                parameters[0] = new Tuple<string, object>("strServiceId", serviceId.ToString());
                parameters[1] = new Tuple<string, object>("strStationName", name);
                parameters[2] = new Tuple<string, object>("strCallSign", callSign);
                parameters[3] = new Tuple<string, object>("strStationDescription", description);
                parameters[4] = new Tuple<string, object>("strAbout", about);
                parameters[5] = new Tuple<string, object>("strStationUnique", unique);

                int rows = await DBFactory.SQL_ExecuteNonQueryAsync(WebApiConfig.ConnectionString, sproc, System.Data.CommandType.StoredProcedure, parameters);

                return Ok(rows > 0);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("api/Channel/Update/{serviceId:int}")]
        public async Task<IHttpActionResult> UpdateChannel(int serviceId, [FromBody]Channel channel)
        {
            try
            {
                string sproc = "sp_FUIUpdateChannel";

                var oldChannel = (await (await (await GetByServiceId(serviceId)).ExecuteAsync(CancellationToken.None)).Content.ReadAsAsync<IEnumerable<Channel>>()).FirstOrDefault();

                if (null == oldChannel)
                    return Content(HttpStatusCode.NotFound, string.Format("Could not find station with service id {0}.", serviceId));

                string unique = string.Empty;

                if ((!string.IsNullOrEmpty(channel.UniqueString) && channel.UniqueString != oldChannel.UniqueString)
                    && (!(string.IsNullOrEmpty(channel.StationName) && string.IsNullOrEmpty(channel.CallSign))))
                {

                    unique = channel.StationName ?? oldChannel.StationName;
                    unique += channel.CallSign ?? oldChannel.CallSign;
                    unique += 300; //Change this with correct feed timezone information
                    unique = unique.Replace(" ", "").Trim();
                }

                channel.StationName = channel.StationName ?? oldChannel.StationName;
                channel.CallSign = channel.CallSign ?? oldChannel.CallSign;
                channel.StationDescription = channel.StationDescription ?? oldChannel.StationDescription;
                channel.UniqueString = string.IsNullOrEmpty(unique) ? oldChannel.UniqueString : unique;

                Tuple<string, object>[] parameters = new Tuple<string, object>[6];

                parameters[0] = new Tuple<string, object>("strServiceId", serviceId.ToString());
                parameters[1] = new Tuple<string, object>("strCallSign", channel.CallSign);
                parameters[2] = new Tuple<string, object>("strStationName", channel.StationName);
                parameters[3] = new Tuple<string, object>("strStationDescription", channel.StationDescription);
                parameters[4] = new Tuple<string, object>("strStationUnique", unique);
                parameters[5] = new Tuple<string, object>("intBitmapId", channel.Logo.BitmapId);

                int rows = await DBFactory.SQL_ExecuteNonQueryAsync(WebApiConfig.ConnectionString, sproc, System.Data.CommandType.StoredProcedure, parameters);

                return Ok(rows > 0);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        #endregion PUT
    }
}
