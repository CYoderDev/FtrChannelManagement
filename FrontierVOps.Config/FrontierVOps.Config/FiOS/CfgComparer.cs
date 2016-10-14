using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.Common.FiOS;

namespace FrontierVOps.Config.FiOS
{
    public class CfgComparer
    {
        /// <summary>
        /// Try to match a string value to a FiOS Role
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static FiOSRole RoleMatch(string value)
        {
            switch(value.ToUpper())
            {
                case "ADMINCONSOLE":
                case "AC":
                    return FiOSRole.AdminConsole;
                case "AES":
                    return FiOSRole.AES;
                case "AUTOPROVISIONING":
                case "AUTOPROVISION":
                case "SELFPROVISIONING":
                case "SELFPROVISION":
                    return FiOSRole.AutoProvisioning;
                case "AVMANAGEMENT":
                case "AVMGMT":
                case "AV-MGMT":
                case "AV":
                    return FiOSRole.AVManagement;
                case "FIOSADVANCEDAIM":
                case "AIM":
                case "FIOSADVAIM":
                case "FIOSADVANCED-AIM":
                case "FIOS-ADVANCED-AIM":
                    return FiOSRole.FiOSAdvancedAIM;
                case "FIOSADVANCEDBANNER":
                case "BANNER":
                case "FIOSADVBANNER":
                case "FIOSADVANCED-BANNER":
                case "FIOS-ADVANCED-BANNER":
                case "BANNERADS":
                    return FiOSRole.FiOSAdvancedBanner;
                case "FIOSONTHEGO":
                case "FOTG":
                    return FiOSRole.FiOSOnTheGo;
                case "HOPPER":
                    return FiOSRole.Hopper;
                case "HYDRA":
                case "TVE":
                case "HYDRA-TVE":
                case "HYDRA/TVE":
                case "TVE-HYDRA":
                case "TVE/HYDRA":
                    return FiOSRole.Hydra;
                case "LOGGING":
                case "STBLOGGING":
                case "STBLOG":
                case "STBLOGGER":
                    return FiOSRole.Logging;
                case "MSV":
                    return FiOSRole.MSV;
                case "NSP":
                case "NOTIFICATIONSERVER":
                    return FiOSRole.NSP;
                case "PLAYREADY":
                    return FiOSRole.Playready;
                case "RATINGSANDRECOMM":
                case "RATINGSANDRECOMMENDATION":
                case "RANDR":
                    return FiOSRole.RatingsAndRecomm;
                case "SEARCH":
                    return FiOSRole.Search;
                case "VOD":
                case "VIDEOONDEMAND":
                case "VODENCRYPTION":
                    return FiOSRole.VOD;
                case "MEDIAMANAGER":
                case "MEDIAMGR":
                case "MMA":
                case "WEBREMOTE-MEDIAMGR":
                case "WEBREMOTE":
                    return FiOSRole.MediaManager;
                case "DOMAINCONTROLLER":
                case "DC":
                    return FiOSRole.DomainController;
                case "KMS-MDT":
                case "KMSORMDT":
                case "MDT-KMS":
                case "MDTORKMS":
                case "KMS":
                case "MDT":
                    return FiOSRole.KMSorMDT;
                case "SCOM":
                case "SCOM-DB":
                case "SCOM-MGMT":
                case "SCOM-GW":
                case "SCOM-RS":
                    return FiOSRole.SCOM;
                case "SCCM":
                    return FiOSRole.SCCM;
                case "SFTP":
                case "FTP":
                    return FiOSRole.SFTP;
                case "GATEWAY":
                    return FiOSRole.Gateway;
                case "IMG":
                case "INTERACTIVEMEDIAGUIDE":
                    return FiOSRole.IMG;
                case "MES":
                    return FiOSRole.MES;
                case "MGS":
                case "GLOBALSEARCH":
                    return FiOSRole.MGS;
                case "THUMBNAIL":
                case "MMG":
                    return FiOSRole.Thumbnail;
                default:
                    return FiOSRole.Unknown;
            }
        }
    }
}
