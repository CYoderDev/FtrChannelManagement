using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.HealthCheck.DataObjects
{
    public class HealthRollup
    {
        public string ServerName { get; set; }
        public StatusResult Result { get; set; }
        public List<string> Errors { get; set; }

        public HealthRollup()
        {
            this.Errors = new List<string>();
        }

        public override bool Equals(object obj)
        {
            var hru = obj as HealthRollup;

            if (hru == null)
                throw new ArgumentException("Incorrect object type");

            if (this.Errors.Count == hru.Errors.Count)
            {
                for (int i = 0; i < this.Errors.Count; i++)
                {
                    if (!this.Errors.Contains(hru.Errors[i]) || !hru.Errors.Contains(this.Errors[i]))
                    {
                        return false;
                    }
                }
            }

            return this.Result.Equals(hru.Result) && this.ServerName.Equals(hru.ServerName);
        }
    }

    public class HealthRollupCollection : ICollection<HealthRollup>
    {
        private List<HealthRollup> _innerHealthRollColl;

        public HealthRollupCollection()
        {
            this._innerHealthRollColl = new List<HealthRollup>();
        }

        public IEnumerator<HealthRollup> GetEnumerator()
        {
            return this._innerHealthRollColl.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
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
                _innerHealthRollColl.Add(item);
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

        public bool IsReadOnly()
        {
            return false;
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
