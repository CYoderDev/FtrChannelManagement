using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using FrontierVOps.Data;
using FrontierVOps.ChannelMapWS.Models;

namespace FrontierVOps.ChannelMapWS.Controllers
{
    [RoutePrefix("api/logo/channel")]
    public class ChannelLogoController : ApiController
    {
        private string _logoDir;

        public ChannelLogoController()
        {
#if DEBUG
            string logoDir = WebConfigurationManager.AppSettings["LogoRepositoryDEV"];
#else
            string logoDir = WebConfigurationManager.AppSettings["LogoRespositoryPROD"];
#endif
        }

        #region GET
        [HttpGet]
        [Route("bitmapId/[bitmapId:int:min(1):max=(9999)]")]
        public ChannelLogo GetByBitmapId(int bitmapId)
        {
            ChannelLogo logo = new ChannelLogo();
            logo.LogoFile = logo.TryGetLogoFile(bitmapId);
            logo.ID = bitmapId;

            if (null != logo.LogoFile)
            {
                using (var bm = new Bitmap(logo.LogoFile.FullName))
                {
                    logo.Image = ChannelLogo.ConvertToBytes(bm);
                }
                return logo;
            }
            return null;
        }
        #endregion GET

        #region PUT

        #endregion PUT

        #region POST

        #endregion POST
    }
}
