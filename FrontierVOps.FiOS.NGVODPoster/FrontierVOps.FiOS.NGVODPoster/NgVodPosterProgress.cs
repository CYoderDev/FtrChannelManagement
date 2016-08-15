using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.NGVODPoster
{
    public class NgVodPosterProgress : IDisposable
    {
        public int Total;
        public int Success;
        public int Failed;
        public int Skipped;
        public int Deleted;
        public Stopwatch Time { get; set; }
        public bool StopProgress { get; set; }
        public bool IsCanceled { get; set; }
        public bool IsComplete { get; set; }

        public NgVodPosterProgress()
        {
            this.Total = 0;
            this.Success = 0;
            this.Failed = 0;
            this.Skipped = 0;
            this.Deleted = 0;
            this.Time = new Stopwatch();
            this.StopProgress = true;
            this.IsCanceled = false;
            this.IsComplete = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Time.IsRunning)
                    this.Time.Stop();
            }
        }
    }
}
