using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace ChannelAPI.Repositories
{
    public interface IRepository<T>
    {
        long Add(T obj);
        Task<long> AddAsync(T obj);
        void Remove(T obj);
        Task RemoveAsync(T obj);
        void Update(T obj);
        Task UpdateAsync(T obj);
        T FindByID(string id);
        Task<T> FindByIDAsync(string id);
        IEnumerable<T> GetAll();
        Task<IEnumerable<T>> GetAllAsync();
    }
}
