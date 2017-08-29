﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChannelAPI.Models
{
    public class StationDTO
    {
        public string strFIOSServiceId { get; set; }
        public string strStationName { get; set; }
        public string strStationUniqueDescription { get; set; }
        public string strStationCallSign { get; set; }
        public string strStationAffiliateId { get; set; }
        public string strCityId { get; set; }
        public string strStateId { get; set; }
        public string strCountyId { get; set; }
        public string strZipCode { get; set; }
        public string strCountryId { get; set; }
        public string strTimeZoneId { get; set; }
        public string strDMAId { get; set; }
        public string strFCCChannelNumber { get; set; }
        public string strStationTypeId { get; set; }
        public int intBitmapId { get; set; }
        public DateTime dtCreateDate { get; set; }
        public DateTime dtLastUpdateDate { get; set; }
        public string strStationOrigin { get; set; }
        public string strDataProviderStationName { get; set; }
        public string strDataProviderStationCallSign { get; set; }
        public string strAbout { get; set; }
        public string strChannelMetadata { get; set; }
        public string strPortalAppID { get; set; }
        public string strProviderAppID { get; set; }
    }
}
