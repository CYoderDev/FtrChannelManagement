using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace ChannelAPI.Models
{
    [Table("tFiosBitmapStationMap")]
    public class BitmapStationMapDTO
    {
        [Key]
        public string strFIOSServiceId { get; set; }
        public int intBitmapId { get; set; }
        public string strFIOSVersionAliasId { get; set; }
        public DateTime dtCreateDate { get; set; }
        public DateTime dtLastUpdateDate { get; set; }
    }
}
