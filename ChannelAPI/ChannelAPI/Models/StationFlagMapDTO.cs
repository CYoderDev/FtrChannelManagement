using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace ChannelAPI.Models
{
    [Table("tFIOSStationFlagMap")]
    public class StationFlagMapDTO
    {
        [Key]
        public string strFIOSServiceId { get; set; }
        [Key]
        public string strStationFlagId { get; set; }
        public string strStationFlagValue { get; set; }
        public DateTime dtCreateDate { get; set; }
        public DateTime dtLastUpdateDate { get; set; }
        [Key]
        public string strfiosversion { get; set; }
        public int intFormatId { get; set; }
    }
}
