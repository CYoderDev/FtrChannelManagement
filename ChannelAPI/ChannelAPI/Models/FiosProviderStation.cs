using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace ChannelAPI.Models
{
    [Table("tFIOSProviderStation")]
    public class FiosProviderStation
    {
        [Key]
        public string strDataProviderId { get; set; }
        [Key]
        public string strProviderStationId { get; set; }
        public string strProviderStationName { get; set; }
        [Key]
        public string strFIOSServiceId { get; set; }
        public DateTime dtCreateDate { get; set; }
        public DateTime dtLastUpdateDate { get; set; }
        public bool isPrimary { get; set; }
    }
}
