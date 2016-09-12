using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FrontierVOps.FiOS.Servers.Objects;

namespace FrontierVOps.FiOS.HealthCheck.DataObjects
{
    public class HealthRollup
    {
        /// <summary>
        /// FiOS Server
        /// </summary>
        public FiOSServer Server { get; set; }

        /// <summary>
        /// Result of the Health Rollup
        /// </summary>
        public StatusResult Result { get; set; }

        /// <summary>
        /// Errors for each type of health check
        /// </summary>
        public List<HealthCheckError> Errors { get; set; }

        public HealthRollup()
        {
            this.Errors = new List<HealthCheckError>();
        }

        public override bool Equals(object obj)
        {
            var hru = obj as HealthRollup;

            if (hru == null)
                throw new ArgumentException("Incorrect object type");         

            return this.Result.Equals(hru.Result) && this.Server.HostName.Equals(hru.Server.HostName);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class HealthRollupCollection : ICollection<HealthRollup>
    {
        private List<HealthRollup> _innerHealthRollColl;
        private ConcurrentBag<HealthRollup> _concHealthRollColl;
        private Func<HealthRollup> _objectGenerator;

        public HealthRollupCollection()
        {
            this._innerHealthRollColl = new List<HealthRollup>();
        }

        public HealthRollupCollection(Func<HealthRollup> objectGenerator)
        {
            if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");

            _concHealthRollColl = new ConcurrentBag<HealthRollup>();
            _objectGenerator = objectGenerator;
        }

        public HealthRollup GetObject()
        {
            HealthRollup hru;
            if (_concHealthRollColl.TryTake(out hru)) return hru;
            return _objectGenerator();
        }

        public void PutObject(HealthRollup item)
        {
            if (!_concHealthRollColl.Contains(item))
            {
                _concHealthRollColl.Add(item);
            }
        }

        public void ConcurrentToList()
        {
            if (this._concHealthRollColl == null)
                throw new Exception("Concurrent bag not initialized");

            this._innerHealthRollColl = new List<HealthRollup>();

            this._innerHealthRollColl = this._concHealthRollColl.GroupBy(x => x.Server).Select(y => new HealthRollup()
                {
                    Server = y.Key,
                    Result = y.Select(x => x.Result).Max(),
                    Errors = y.SelectMany(x => x.Errors).GroupBy(x => x.HCType)
                    .Select(x => new HealthCheckError()
                        {
                           HCType = x.Key,
                           Error = x.Where(z => z.HCType == x.Key).SelectMany(z => z.Error).ToList(),
                        }).ToList(),
                }).ToList();
        }

        public IEnumerator<HealthRollup> GetEnumerator()
        {
            if (this._innerHealthRollColl == null)
                return this._concHealthRollColl.GetEnumerator();
            return this._innerHealthRollColl.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this._innerHealthRollColl == null)
                return this._concHealthRollColl.GetEnumerator();
            return this._innerHealthRollColl.GetEnumerator();
        }

        public HealthRollup this[int index]
        {
            get { return (HealthRollup)_innerHealthRollColl[index]; }
            set { _innerHealthRollColl[index] = value; }
        }

        public bool Contains(HealthRollup item)
        {
            bool found = false;

            _innerHealthRollColl.ForEach((innerHru) =>
                {
                    if (innerHru.Equals(item))
                        found = true;
                });

            return found;
        }

        public void Add(HealthRollup item)
        {
            if (!Contains(item))
            {
                _innerHealthRollColl.Add(item);
            }
        }

        public void Clear()
        {
            _innerHealthRollColl.Clear();
        }

        public int Count
        {
            get { return _innerHealthRollColl.Count; }
        }

        public bool Remove(HealthRollup item)
        {
            for (int i = 0; i < _innerHealthRollColl.Count; i++)
            {
                if (_innerHealthRollColl[i].Equals(item))
                {
                    _innerHealthRollColl.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void CopyTo(HealthRollup[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("The array cannot be null");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("The starting index must not be negative");
            if (Count > array.Length - arrayIndex + 1)
                throw new ArgumentException("The destination array has fewer elements than the collection");

            for (int i = 0; i < _innerHealthRollColl.Count; i++)
                array[i + arrayIndex] = _innerHealthRollColl[i];
        }
    }
}
