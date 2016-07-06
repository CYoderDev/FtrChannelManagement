using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web;
using FrontierVOps.Data;
using FrontierVOps.ChannelMapWS.Models;

namespace FrontierVOps.ChannelMapWS.Controllers
{
    public class ChannelController : ApiController
    {
        #region GET
        [Route("api/FiOS/Channel/ServiceId/{serviceId:int}")]
        public IEnumerable<Channel> GetByServiceId(int serviceId, double version = 1.9)
        {
            string command = "SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "' AND strFIOSServiceId = '" + serviceId.ToString() + "'";
            return Get(command);
        }

        [Route("api/FiOS/Channel/VHO/{vhoId:int:min(1)}")]
        public IEnumerable<Channel> GetByVHO(int vhoId, double version = 1.9)
        {
            string command = "SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "' AND strVHOId = 'VHO" + vhoId.ToString() + "'";
            return Get(command);
        }

        [Route("api/FiOS/Channel/Region/{regionId:int}")]
        public IEnumerable<Channel> GetbyRegion(int regionId, double version = 1.9)
        {
            string command = "SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "' AND strFIOSRegionId = '" + regionId.ToString() + "'";
            return Get(command);
        }

        [Route("api/FiOS/Channel/Logo/{logoId:int}")]
        public IEnumerable<Channel> GetbyLogo(int logoId, double version = 1.9)
        {
            string command = "SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "' AND intBitmapId = " + logoId;
            return Get(command);
        }

        [Route("api/FiOS/Channel/{position:int:min(1)}")]
        public IEnumerable<Channel> GetbyPosition(int position, double version = 1.9)
        {
            string command = "SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version.ToString() + "' AND intChannelPosition = " + position;
            return Get(command);
        }

        [Route("api/FiOS/Channel/query")]
        public IEnumerable<Channel> GetAllChannels(int? serviceId = null, int? position = null, int? logoId = null, int? vhoId = null, int? regionId = null, string stationName = "", string callSign = "", string regionName = "", double version = 1.9)
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

            return Get(command.ToString());
        }

        [Route("api/FiOS/Channel/Logo/Missing")]
        public IEnumerable<Channel> GetNoLogo(int? vhoId = null, int? regionId = null, string regionName = "", double version = 1.9)
        {
            StringBuilder command = new StringBuilder();
            command.Append("SELECT * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version + "' AND intBitmapId = " + 10000);

            if (null != vhoId)
                command.Append(" AND strVHOId = 'VHO" + vhoId + "'");
            if (null != regionId)
                command.Append(" AND strFIOSRegionId = '" + regionId + "'");
            if (!string.IsNullOrEmpty(regionName))
                command.Append(" AND strFIOSRegionName LIKE '" + regionName + "'");

            return Get(command.ToString());
        }

        private IEnumerable<Channel> Get(string command)
        {
            foreach (var record in DBFactory.SQL_ExecuteReader(WebApiConfig.ConnectionString, command, System.Data.CommandType.Text))
            {
                var chan = new Channel();

                chan.ServiceID = int.Parse(record.GetString(0));
                chan.ChannelNumber = record.GetInt32(1);
                chan.CallSign = record.GetString(2);
                chan.StationName = record.GetString(3);
                chan.Logo.ID = record.GetInt32(4);
                chan.Version = record.GetString(5);
                chan.VHO = record.GetString(6);
                chan.LastUpdate = record.IsDBNull(7) ? (DateTime?)null : record.GetDateTime(7);
                chan.RegionID = int.Parse(record.GetString(8));
                chan.RegionName = record.IsDBNull(9) ? null : record.GetString(9);
                chan.StationDescription = record.IsDBNull(10) ? null : record.GetString(10);
                chan.UniqueString = record.IsDBNull(11) ? null : record.GetString(11);

                if (chan.Logo.ID != null && chan.Logo.ID.HasValue)
                    chan.Logo.LogoFile = chan.Logo.TryGetLogoFile(chan.Logo.ID.Value);

                yield return chan;
            }
        }
        #endregion GET

        #region PUT
        [HttpPut]
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("api/Channel/Update/CallSign/[serviceId]")]
        public bool UpdateCallSign(int serviceId, string callSign)
        {
            string sproc = "sp_FUIUpdateStation";

            Tuple<string,object>[] parameters = new Tuple<string,object>[2];

            parameters[0] = new Tuple<string,object>("strServiceId", serviceId.ToString());
            parameters[1] = new Tuple<string,object>("strCallSign", callSign);

            int rows = DBFactory.SQL_ExecuteNonQuery(WebApiConfig.ConnectionString, sproc, System.Data.CommandType.StoredProcedure, parameters);

            return rows > 0;
        }

        [HttpPut]
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("api/Channel/Update/Station/[serviceId]")]
        public bool UpdateStation(int serviceId, string name = null, string callSign = null, string description = null, string about = null)
        {
            string sproc = "sp_FUIUpdateStation";

            string unique = null;

            if (!(string.IsNullOrEmpty(name) && string.IsNullOrEmpty(callSign)))
            {
                unique = name ?? GetByServiceId(serviceId).FirstOrDefault().StationName;
                unique += callSign ?? GetByServiceId(serviceId).FirstOrDefault().CallSign;
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

            int rows = DBFactory.SQL_ExecuteNonQuery(WebApiConfig.ConnectionString, sproc, System.Data.CommandType.StoredProcedure, parameters);

            return rows > 0;
        }
        #endregion PUT
    }
}
