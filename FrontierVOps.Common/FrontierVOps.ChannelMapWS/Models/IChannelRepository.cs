using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.ChannelMapWS.Models
{
    interface IChannelRepository
    {
        void Add(Channel Chan);
        Channel Remove(int ServiceID);
        IEnumerable<Channel> GetAll();
        Channel Find(int ServiceID);
        void Update(Channel Chan);
    }
}
