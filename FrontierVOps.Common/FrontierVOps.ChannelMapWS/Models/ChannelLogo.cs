using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace FrontierVOps.ChannelMapWS.Models
{
    public class ChannelLogo : Logo
    {
        public int? ID { get; set; }
        public FileInfo LogoFile { get; set; }

        #region PublicMethods
        
        public FileInfo TryGetLogoFile(int logoId)
        {
#if DEBUG
            string logoDir = WebConfigurationManager.AppSettings["LogoRepositoryDEV"];
#else
            string logoDir = WebConfigurationManager.AppSettings["LogoRespositoryPROD"];
#endif
            string filePath = Directory.EnumerateFiles(logoDir, logoId + ".png", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (filePath == null)
                return null;

            return new FileInfo(filePath);
        }

        #endregion

    }
}