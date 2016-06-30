﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FrontierVOps.ChannelMapWS.Models
{
    public class Channel
    {
        public int ServiceID { get; set; }
        public int ChannelNumber { get; set; }
        public string StationName { get; set; }
        public string CallSign { get; set; }
        public ChannelLogo Logo { get; set; }
        public string Version { get; set; }
        public string VHO { get; set; }
        public DateTime LastUpdate { get; set; }
        public int RegionID { get; set; }
        public string RegionName { get; set; }
        public string StationDescription { get; set; }
        public string UniqueString { get; set; }
    }
}