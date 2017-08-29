using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiosChannelManager.DataObjects
{
    public class FiOSChannel
    {
        public string FiosServiceId { get; set; }
        public int? ChannelPosition { get; set; }
        public string CallSign { get; set; }
        public string StationName { get; set; }
        public int? BitmapId { get; set; }
        public string FIOSVersionAliasId { get; set; }
        public string VHOId { get; set; }
        public DateTime? BitmapCreateDate { get; set; }
        public string RegionId { get; set; }
        public string RegionName { get; set; }
        public string StationDescription { get; set; }
        public string DataProviderStationName { get; set; }
        public string StationFlagId { get; set; }
        public string StationFlagValue { get; set; }
        public string StationGenre { get; set; }
        public string TMSId { get; set; }

        public override string ToString()
        {
            string strChanPosition = ChannelPosition.HasValue ? this.ChannelPosition.Value.ToString() : "NULL";
            string strBitmapId = BitmapId.HasValue ? this.BitmapId.Value.ToString() : "NULL";
            string strBitmapCreateDate = BitmapCreateDate.HasValue ? this.BitmapCreateDate.Value.ToString("MM/dd/yyyy hh:mm:ss tt") : "NULL";
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("\tFiosServiceId: {0}\n\tChannel #: {1}\n\tCallSign: {2}\n\tStation Name: {3}\n\t",
                this.FiosServiceId, strChanPosition, this.CallSign, this.StationName);
            sb.AppendFormat("Bitmap Id: {0}\n\tFiOS Version: {1}\n\tVHO: {2}\n\tLogo Date: {3}\n\t",
                strBitmapId, this.FIOSVersionAliasId, this.VHOId, strBitmapCreateDate);
            sb.AppendFormat("Region Id: {0}\n\tRegion Name: {1}\n\tStation Desc: {2}\n\tProvider Station Name: {3}\n\t",
                this.RegionId, this.RegionName, this.StationDescription, this.DataProviderStationName);
            sb.AppendFormat("TMS Id: {0}\n\tStation Genre{1}\n\tStation Flag Value:{2}",
                this.TMSId, this.StationGenre, this.StationFlagValue);

            return sb.ToString();
        }
    }
}
