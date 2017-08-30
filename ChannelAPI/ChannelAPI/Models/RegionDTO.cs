using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace ChannelAPI.Models
{
    [Table("tFIOSRegion")]
    public class RegionDTO
    {
        [Key]
        public string strFIOSRegionId { get; set; }
        public string strFIOSRegionName { get; set; }
        public string strVHOId { get; set; }
        public string strCommunityId { get; set; }
        public string strStateCountyId { get; set; }
        public string strZipCode { get; set; }
        public string strDMAId { get; set; }
        public string strLocationId { get; set; }
        public string strTimeZoneId { get; set; }
        public string strVirtualChannelPosition { get; set; }
        public DateTime dtCreateDate { get; set; }
        public DateTime dtLastUpdateDate { get; set; }
        public string strNeedsDSTCorrection { get; set; }
    }
}
