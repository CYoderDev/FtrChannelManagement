using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FrontierVOps.Common.FiOS;
using FrontierVOps.Config.FiOS;
using FrontierVOps.Data.Objects;

namespace FrontierVOps.Data
{
    public class DBConfig
    {
        private static XDocument _config { get; set; }
        private static XNamespace _ns { get; set; }

        static DBConfig()
        {
            if (DBConfig._config == null)
            {
                CfgHelper cfgHelper = new CfgHelper();
                DBConfig._config = cfgHelper.GetConfig("Database.xml");
                DBConfig._ns = _config.Root.GetDefaultNamespace();
            }
        }

        public static IEnumerable<Datasource> GetDBs()
        {
            var dsEles = _config.Root.Elements(_ns + "DataSource");

            foreach(var dsEle in dsEles)
            {
                var ds = new Datasource(dsEle.Attribute("Name").Value);
                ds.IP = dsEle.Attribute("IP").Value;
                ds.Type = getDSType(dsEle.Attribute("Type").Value);
                ds.Role = getRole(dsEle.Element(_ns + "Role"));
                
                foreach (var dbEle in dsEles.Elements(_ns + "Database"))
                {
                    if (ds.Type == DSType.TSQL)
                    {
                        iDatabase db = new SqlDb();
                        db.DatabaseName = dbEle.Attribute("Name").Value;

                        var functEle = dbEle.Element(_ns + "Function");

                        switch (functEle.Attribute("Location").Value.ToUpper())
                        {
                            case "VHE":
                                db.Location = FiOSLocation.VHE;
                                break;
                            case "VHO":
                                db.Location = FiOSLocation.VHO;
                                break;
                            default:
                                db.Location = FiOSLocation.Unknown;
                                break;
                        }

                        switch (functEle.Value.ToUpper())
                        {
                            case "ADMIN":
                                db.Function = DbFunction.Admin;
                                break;
                            case "APPLICATION":
                                db.Function = DbFunction.Application;
                                break;
                            case "LOGGING":
                                db.Function = DbFunction.Logging;
                                break;
                            default:
                                db.Function = DbFunction.Unknown;
                                break;
                        }

                        ds.Databases.Add(db);
                    }
                }
                yield return ds;
            }
        }

        private static FiOSRole getRole(XElement roleEle)
        {
            var role = roleEle.Elements().FirstOrDefault().Name.LocalName.ToUpper();
            var val = roleEle.Elements().FirstOrDefault().Value;
            if (role.Equals("INFRASTRUCTURE"))
            {
                return CfgComparer.RoleMatch(val);
            }
            return CfgComparer.RoleMatch(role);
        }

        private static DSType getDSType(string value)
        {
            switch (value.ToUpper())
            {
                case "TSQL":
                    return DSType.TSQL;
                case "MONGO":
                    return DSType.Mongo;
                case "CASSANDRA":
                case "CASS":
                    return DSType.Cassandra;
                default:
                    return DSType.TSQL;
            }
        }
    }
}
