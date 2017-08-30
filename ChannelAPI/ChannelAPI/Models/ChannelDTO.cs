using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace ChannelAPI.Models
{
    [Table("vChannels")]
    public class ChannelDTO
    {
        public string strFIOSServiceId { get; set; }
        public string strStationCallSign { get; set; }
        public string strStationName { get; set; }
        public int intBitMapId { get; set; }
        public string strFIOSVersionAliasId { get; set; }
        public string strVHOId { get; set; }
        public DateTime dtCreateDate { get; set; }
        public string strFIOSRegionId { get; set; }
        public string strFIOSRegionName { get; set; }
        public string strStationDescription { get; set; }
        public string strDataProviderStationName { get; set; }
        public int iStationGenreID { get; set; }
        public string strStationFlagId { get; set; }
        public string strStationFlagValue { get; set; }
        public string TMSId { get; set; }
    }
}
