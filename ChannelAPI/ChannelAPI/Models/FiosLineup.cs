using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChannelAPI.Models
{
    public class FiosLineup
    {
        public string strFIOSRegionId { get; set; }
        public string strFIOSServiceId { get; set; }
        public int intChannelPosition { get; set; }
        public string strLineupDays { get; set; }
        public string strConverterBoxTypeId { get; set; }
        public string strServiceTierId { get; set; }
        public DateTime dtLineupEffectiveDate { get; set; }
        public DateTime dtLineupExpiryDate { get; set; }
        public DateTime dtCreateDate { get; set; }
        public DateTime dtLastUpdateDate { get; set; }
        public string strActualFIOSServiceID { get; set; }
        public int iStationGenreID { get; set; }
    }
}
