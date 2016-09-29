using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.NGVODPoster
{
    public class SourceFileComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return Path.GetFileName(x) == Path.GetFileName(y);
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
}
