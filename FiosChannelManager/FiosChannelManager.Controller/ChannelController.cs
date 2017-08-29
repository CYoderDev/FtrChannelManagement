using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.Data;
using FiosChannelManager.DataObjects;

namespace FiosChannelManager.Controller
{
    public class ChannelController
    {
        public static string ConnectionString { get { return ChannelController._connectionString; } set { ChannelController._connectionString = value; } }
        private static string _connectionString;

        public ChannelController()
        { 
        }

        public static IEnumerable<FiOSChannel> GetChannels()
        {
            string sproc = "sp_FIOSGetChannelLogos";

            foreach (var dr in DBFactory.SQL_ExecuteReader(ChannelController._connectionString, sproc, System.Data.CommandType.StoredProcedure))
            {
                var fiosChannel = new FiOSChannel();
                fiosChannel.FiosServiceId = dr.GetString(0);
                fiosChannel.ChannelPosition = dr.GetInt32(1);
                fiosChannel.CallSign = dr.GetString(2);
                fiosChannel.StationName = dr.GetString(3);
                fiosChannel.BitmapId = dr.IsDBNull(4) ? 10000 : dr.GetInt32(4);
                fiosChannel.FIOSVersionAliasId = dr.IsDBNull(5) ? null : dr.GetString(5);
                fiosChannel.VHOId = dr.GetString(6);
                if (dr.IsDBNull(6))
                    fiosChannel.BitmapCreateDate = null;
                else
                    fiosChannel.BitmapCreateDate = dr.GetDateTime(6);
                fiosChannel.RegionId = dr.GetString(7);
                fiosChannel.RegionName = dr.GetString(8);
                fiosChannel.StationDescription = dr.IsDBNull(9) ? null : dr.GetString(9);
                fiosChannel.DataProviderStationName = dr.IsDBNull(10) ? null : dr.GetString(10);
                fiosChannel.StationFlagId = dr.IsDBNull(11) ? null : dr.GetString(11);
                fiosChannel.StationFlagValue = dr.IsDBNull(12) ? null : dr.GetString(12);
                fiosChannel.StationGenre = dr.IsDBNull(13) ? null : dr.GetString(13);
                fiosChannel.TMSId = dr.GetString(14);
                
                yield return fiosChannel;
            }
        }

        public static IEnumerable<string> GetRegionIds()
        {
            return GetChannels().Select(x => x.RegionId).Distinct();
        }

        public static string GetFiosServiceId(int ChannelNumber, string RegionId)
        {
            return GetChannels().Where(x => x.ChannelPosition == ChannelNumber && x.RegionId == RegionId).Select(x => x.FiosServiceId).FirstOrDefault();
        }
    }
}
