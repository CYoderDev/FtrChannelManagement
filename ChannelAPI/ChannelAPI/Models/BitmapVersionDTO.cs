﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace ChannelAPI.Models
{
    [Table("tFIOSBitmapVersion")]
    public class BitmapVersionDTO
    {
        [ExplicitKey]
        public int intBitmapId { get; set; }
        public string strBitmapMD5Digest { get; set; }
        [ExplicitKey]
        public string strFIOSVersionAliasId { get; set; }
        public DateTime dtCreateDate { get; set; }
        public DateTime dtLastUpdateDate { get; set; }
        public string strBitMapIsfOffline_YN { get; set; }
        public DateTime dtImageLastUpdateTimestamp { get; set; }
        public string strBitMapIsfOffline_YN_HD { get; set; }
    }
}
