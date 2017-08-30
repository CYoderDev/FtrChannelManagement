using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace ChannelAPI.Models
{
    [Table("tfiosStationGenre")]
    public class StationGenreDTO
    {
        [Key]
        public int iStationGenreId { get; set; }
        public string strStationGenre { get; set; }
        public DateTime dtCreateDate { get; set; }
        public DateTime dtLastUpdateDate { get; set; }
        public int iPriority { get; set; }
        public bool IsHD { get; set; }
        public int iEquivelentGenre { get; set; }
    }
}
