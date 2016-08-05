using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.NGVODPoster
{
    public class VODAsset
    {
        public int AssetId { get; set; }
        public string Title { get; set; }
        public string PID { get; set; }
        public string PAID { get; set; }
        public string PosterSource { get; set; }
        public string PosterDest { get; set; }
        public List<VODFolder> Folders { get; set; }

        public VODAsset()
        {
            this.Folders = new List<VODFolder>();
        }
    }
}
