using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FrontierVOps.ChannelMapWS.Models
{
    public class Channel
    {
        [Key]
        public int ServiceID { get; set; }
        public int ChannelNumber { get; set; }
        public string StationName { get; set; }
        public string CallSign { get; set; }
        public ChannelLogoInfo Logo { get; set; }
        public string Version { get; set; }
        public string VHO { get; set; }
        public DateTime? LastUpdate { get; set; }
        public int RegionID { get; set; }
        public string RegionName { get; set; }
        public string StationDescription { get; set; }
        public string UniqueString { get; set; }

        public Channel()
        {
            this.Logo = new ChannelLogoInfo();
        }
    }
}