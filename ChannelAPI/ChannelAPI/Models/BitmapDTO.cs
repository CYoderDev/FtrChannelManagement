using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace ChannelAPI.Models
{
    [Table("tFIOSBitMap")]
    public class BitmapDTO
    {
        [Key]
        public int intBitmapId { get; set; }
        public string strBitMapName { get; set; }
        public string strBitMapDescription { get; set; }
        public string strBitMapFileName { get; set; }
        public string strBitMapMD5Digest { get; set; }
        public char? strBitMapIsfOffline_YN { get; set; }
        public string strBitMapPreCache_YN { get; set; }
        public string strIsDeleted { get; set; }
        public DateTime dtCreateDate { get; set; }
        public DateTime dtLastUpdateDate { get; set; }
        public string strBitMapConstant { get; set; }
        public int intBitmapSize { get; set; }
        public char? strBitMapIsfOffline_YN_HD { get; set; }
    }
}
