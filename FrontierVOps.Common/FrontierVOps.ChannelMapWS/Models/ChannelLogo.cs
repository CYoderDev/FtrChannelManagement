using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;

namespace FrontierVOps.ChannelMapWS.Models
{
    public class ChannelLogo : Bitmap
    {
        public int ID { get; set; }
        public FileInfo LogoFile { get; set; }
    }
}