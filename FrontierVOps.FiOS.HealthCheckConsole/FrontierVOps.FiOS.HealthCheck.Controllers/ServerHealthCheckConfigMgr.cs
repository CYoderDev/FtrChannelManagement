using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FrontierVOps.Common;
using FrontierVOps.Config.FiOS;
using FrontierVOps.FiOS.Servers.Objects;
using FrontierVOps.FiOS.HealthCheck.DataObjects;

namespace FrontierVOps.FiOS.Servers.Controllers
{
    public class ServerHealthCheckConfigMgr
    {
        static XDocument _config { get; set; }
        static XNamespace _ns { get; set; }

        static ServerHealthCheckConfigMgr()
        {
            if (ServerHealthCheckConfigMgr._config == null)
            {
                CfgHelper cfgHelper = new CfgHelper();
                ServerHealthCheckConfigMgr._config = cfgHelper.GetConfig("HealthCheck.xml");
                ServerHealthCheckConfigMgr._ns = _config.Root.GetDefaultNamespace();
            }
        }

        public static IEnumerable<HCWinService> GetWindowsServicesToCheck()
        {
            var serviceElems = _config.Root.Descendants(_ns + "Services");

            foreach (var serviceElem in serviceElems.Elements())
            {
                HCWinService winService = new HCWinService();
                winService.Function = ServerFunction.Unknown;

                if (serviceElem.HasAttributes)
                {
                    winService.Name = serviceElem.Attribute("Name").Value;
                    winService.DisplayName = serviceElem.Attribute("DisplayName").Value;
                }
                else
                {
                    throw new FormatException("XML Format error. Attributes missing on WindowsService element.");
                }

                //Check for defaults
                if (serviceElem.HasElements && serviceElem.Elements().Any(x => x.Name.LocalName.ToUpper().Equals("DEFAULTS")))
                {
                    var defaultsEle = serviceElem.Element(_ns + "Defaults");

                    foreach(var def in defaultsEle.Elements())
                    {
                        switch (def.Name.LocalName.ToUpper())
                        {
                            case "STATUS":
                                {
                                    switch (def.Value.ToUpper())
                                    {
                                        case "RUNNING":
                                            winService.CheckStatus.Add(ServiceControllerStatus.Running);
                                            break;
                                        case "STOPPED":
                                            winService.CheckStatus.Add(ServiceControllerStatus.Stopped);
                                            break;
                                        case "PAUSED":
                                            winService.CheckStatus.Add(ServiceControllerStatus.Paused);
                                            break;
                                        default:
                                            throw new FormatException(string.Format("XML format error. {0} is not a valid windows service status for {1}", def.Value, winService.Name));
                                    }
                                    break;
                                }
                            case "STARTUPTYPE":
                                {
                                    switch (def.Value.ToUpper())
                                    {
                                        case "AUTOMATIC":
                                            winService.CheckStartupType.Add(ServiceStartMode.Automatic);
                                            break;
                                        case "MANUAL":
                                            winService.CheckStartupType.Add(ServiceStartMode.Manual);
                                            break;
                                        case "DISABLED":
                                            winService.CheckStartupType.Add(ServiceStartMode.Disabled);
                                            break;
                                        default:
                                            throw new FormatException(string.Format("XML format error. {0} is not a valid windows service start mode for {1}", def.Value, winService.Name));
                                    }
                                    break;
                                }
                            case "LOGONAS":
                                {
                                    winService.CheckLogonAs.Add(Toolset.RemoveWhitespace(def.Value).ToUpper());
                                }
                                break;
                            default:
                                throw new FormatException(string.Format("XML format error. {0} is not a valid child element for defaults.", def.Name.LocalName));
                        } //End Switch - Default Element Name
                    } //End Foreach - Default Element

                    try
                    {
                        if (defaultsEle.HasAttributes && defaultsEle.FirstAttribute.Name.LocalName.ToUpper().Equals("ONEPERGROUP"))
                            winService.OnePerGroup = bool.Parse(defaultsEle.FirstAttribute.Value);
                    }
                    catch
                    {
                        throw new FormatException(string.Format("XML format error. Invalid value for defaults attribute ONEPERGROUP for windows service {0}", winService.Name));
                    }
                } //End If - Defaults Check
                var svcElems = serviceElem.Elements();
                //Check for Includes
                if (serviceElem.HasElements && serviceElem.Elements().Any(x => x.Name.LocalName.ToUpper().Equals("INCLUDE")))
                {
                    processIncludeExclude(ref winService, serviceElem.Element(_ns + "Include"), true);
                }

                //Check for Excludes
                if (serviceElem.HasElements && serviceElem.Elements().Any(x => x.Name.LocalName.ToUpper().Equals("EXCLUDE")))
                {
                    processIncludeExclude(ref winService, serviceElem.Element(_ns + "Exclude"), false);
                }

                yield return winService;
            } //End Foreach - Service Element
        }

        public static bool IsExempt(FiOSServer Server, ExemptionType ExemptType, params string[] Args)
        {
            var exemptionsEle = _config.Root.Descendants(_ns + "Exemptions");
            var exemptTypeEle = exemptionsEle.Elements().Where(x => x.Name.LocalName.ToUpper() == ExemptType.ToString().ToUpper());

            if (exemptTypeEle.Count() == 0)
                return false;

            if (exemptTypeEle.Descendants().Any(x => x.Name.LocalName.ToUpper().Equals("SERVER")) && !exemptTypeEle.Descendants(_ns + "Server").Attributes("Name").Select(x => x.Value).Any(x => x.Equals(Server.HostName)))
            {
                return false;
            }

            if (ExemptType == ExemptionType.HardDrive)
            {
                var driveLetters = exemptTypeEle.Elements(_ns + "DriveLetter").Select(x => x.Value);

                bool containsAll = true;
                foreach(var dl in driveLetters)
                {
                    if (!(Args.Contains(dl) || Args.Contains(dl + ":")) && containsAll)
                    {
                        containsAll = false;
                    }
                }
                return containsAll;
            }
            else if (ExemptType == ExemptionType.IIS)
            {
                return true;
            }

            return false;
        }

        private static void processIncludeExclude(ref HCWinService winService, XElement elem, bool isInclude)
        {
            foreach (var ele in elem.Elements())
            {
                switch (ele.Name.LocalName.ToUpper())
                {
                    case "FUNCTION":
                        {
                            if (!ele.HasAttributes)
                            {
                                throw new FormatException("XML format error. Function element for {0} must contain attributes");
                            }

                            if (ele.Attributes().Any(x => x.Name.LocalName.ToUpper().Equals("ALL")))
                            {
                                foreach (var allAttr in ele.Attributes().Where(x => x.Name.LocalName.ToUpper().Equals("ALL")))
                                {
                                    winService.Function = getServerFunction(allAttr.Value);
                                }
                            }
                            break;
                        }
                    case "ROLES":
                        {
                            winService.Roles.AddRange(processWinServiceRoles(isInclude, ele));
                            break;
                        }
                    case "SERVERS":
                        {
                            foreach (var svrEle in ele.Descendants(_ns + "Server"))
                            {
                                var fs = new FiOSServer();
                                fs.HostName = svrEle.Attribute("Name").Value;
                                fs.IPAddress = svrEle.Attribute("IP").Value;
                                fs.HostLocation = svrEle.Attribute("Domain").Value.ToUpper().Contains("VHE") ? ServerLocation.VHE :
                                    svrEle.Attribute("Domain").Value.ToUpper().Contains("VHO") ? ServerLocation.VHO : ServerLocation.Unknown;
                                //if (svrEle.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("EXCLUDE")))
                                //    winService.Servers.Add(new Tuple<bool, FiOSServer>(false, fs));
                                //else
                                    winService.Servers.Add(new Tuple<bool, FiOSServer>(isInclude, fs));
                            }
                            break;
                        }
                }//End include child element switch
            } //End foreach include element
        }

        private static List<Tuple<bool, ServerRole, ServerFunction>> processWinServiceRoles(bool isInclude, XElement rolesEle)
        {
            if (!rolesEle.HasElements)
            {
                return new List<Tuple<bool,ServerRole,ServerFunction>>();
            }

            var retVal = new List<Tuple<bool,ServerRole,ServerFunction>>();

            if (rolesEle.Elements().Any(x => x.Name.LocalName.ToUpper().Equals("ROLE")))
            {
                var _otherNS = rolesEle.Elements().Select(x => x.GetDefaultNamespace()).FirstOrDefault();
                foreach (var roleEle in rolesEle.Element(_otherNS + "Role").Elements())
                {
                    switch (roleEle.Name.LocalName.ToUpper())
                    {
                        case "IMG":
                            retVal.Add(new Tuple<bool,ServerRole,ServerFunction>(isInclude, ServerRole.IMG, getServerFunction(roleEle.Value)));
                            break;
                        case "MES":
                            retVal.Add(new Tuple<bool,ServerRole,ServerFunction>(isInclude, ServerRole.MES, getServerFunction(roleEle.Value)));
                            break;
                        case "MGS":
                            retVal.Add(new Tuple<bool,ServerRole,ServerFunction>(isInclude, ServerRole.MGS, getServerFunction(roleEle.Value)));
                            break;
                        case "ADMINCONSOLE":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.AdminConsole, getServerFunction(roleEle.Value)));
                            break;
                        case "AES":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.AES, getServerFunction(roleEle.Value)));
                            break;
                        case "AUTOPROVISION":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.AutoProvisioning, getServerFunction(roleEle.Value)));
                            break;
                        case "FIOSADVANCED-AIM":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.FiOSAdvancedAIM, getServerFunction(roleEle.Value)));
                            break;
                        case "FIOSADVANCED-BANNER":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.FiOSAdvancedBanner, getServerFunction(roleEle.Value)));
                            break;
                        case "FOTG":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.FiOSOnTheGo, getServerFunction(roleEle.Value)));
                            break;
                        case "TVE-HYDRA":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.Hydra, getServerFunction(roleEle.Value)));
                            break;
                        case "MSV":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.MSV, getServerFunction(roleEle.Value)));
                            break;
                        case "NSP":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.NSP, getServerFunction(roleEle.Value)));
                            break;
                        case "PLAYREADY":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.Playready, getServerFunction(roleEle.Value)));
                            break;
                        case "RATINGSANDRECOMM":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.RatingsAndRecomm, getServerFunction(roleEle.Value)));
                            break;
                        case "SEARCH":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.Search, getServerFunction(roleEle.Value)));
                            break;
                        case "LOGGING":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.Logging, getServerFunction(roleEle.Value)));
                            break;
                        case "VOD":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.VOD, getServerFunction(roleEle.Value)));
                            break;
                        case "WEBREMOTE-MEDIAMGR":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.MediaManager, getServerFunction(roleEle.Value)));
                            break;
                        case "INFRASTRUCTURE":
                            {
                                switch (roleEle.Value.ToUpper())
                                {
                                    case "DOMAINCONTROLLER":
                                        retVal.Add(new Tuple<bool,ServerRole,ServerFunction>(isInclude, ServerRole.DomainController, ServerFunction.Infrastructure));
                                        break;
                                    case "AV-MGMT":
                                        retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.AVManagement, ServerFunction.Infrastructure));
                                        break;
                                    case "HOPPER":
                                        retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.Hopper, ServerFunction.Infrastructure));
                                        break;
                                    case "SCOM-DB":
                                        retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.SCOM, ServerFunction.Database));
                                        break;
                                    case "SCOM-MGMT":
                                        retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.SCOM, ServerFunction.Application));
                                        break;
                                    case "SCOM-GW":
                                    case "SCOM-RS":
                                        retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.SCOM, ServerFunction.Infrastructure));
                                        break;
                                    case "SCCM":
                                        retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.SCCM, ServerFunction.Infrastructure));
                                        break;
                                    case "SFTP":
                                        retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.SFTP, ServerFunction.Infrastructure));
                                        break;
                                    default:
                                        throw new FormatException("XML Format Error. Invalid infrastructure value.");
                                }
                                break;
                            }
                        case "GATEWAY":
                            retVal.Add(new Tuple<bool, ServerRole, ServerFunction>(isInclude, ServerRole.Gateway, ServerFunction.Infrastructure));
                            break;
                        default:
                            throw new FormatException("XML Format Error. Invalid role element.");
                    }// End Role Switch
                }// End foreach role element loop
            } // End if statement
            return retVal;
        } // End processWinServiceRoles method

        private static ServerFunction getServerFunction(string functionValue)
        {
            switch (functionValue.ToUpper())
            {
                case "DATABASE":
                    return ServerFunction.Database;
                case "WEB":
                    return ServerFunction.Web;
                case "APPLICATION":
                    return ServerFunction.Application;
                case "INFRASTRUCTURE":
                    return ServerFunction.Infrastructure;
                default:
                    return ServerFunction.Unknown;
            }
        }
    }
}
