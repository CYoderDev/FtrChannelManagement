using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.NGVODPoster
{
    public class VODFolder
    {
        public int ID { get; set; }
        public int ParentId { get; set; }
        public string Path { get; set; }
        public string Title { get; set; }
        public List<VODAsset> VodAssets { get; set; }
    }
}
