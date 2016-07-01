using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace FrontierVOps.ChannelMapWS.Models
{
    public class UnitOfWork : IDisposable
    {
        private FIOSApp_DC_TestEntities _context = null;
        private ChannelRepository<Channel> _channelRepository;
        private ChannelRepository<ChannelLogo> _logoRepository;

        public UnitOfWork()
        {
            _context = new FIOSApp_DC_TestEntities();
        }

        public ChannelRepository<Channel> ChannelRepo
        {
            get
            {
                if (this._channelRepository == null)
                    this._channelRepository = new ChannelRepository<Channel>(_context);
                return _channelRepository;
            }
        }

        public ChannelRepository<ChannelLogo> LogoRepo
        {
            get
            {
                if (this._logoRepository == null)
                    this._logoRepository = new ChannelRepository<ChannelLogo>(_context);
                return _logoRepository;
            }
        }

        public void Save()
        {
            try
            {
                _context.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                var outputLines = new List<string>();
                foreach (var eve in ex.EntityValidationErrors)
                {
                    outputLines.Add(string.Format("{0}: Entity of type \"{1}\" in state \"{2}\" has the following validation errors:",
                        DateTime.Now,
                        eve.Entry.Entity.GetType().Name,
                        eve.Entry.State));
                    foreach (var ve in eve.ValidationErrors)
                        outputLines.Add(string.Format("- Property: \"{0}\", Error: \"{1}\"", ve.PropertyName, ve.ErrorMessage));
                }

                System.IO.File.AppendAllLines(@"C:\FiOSVSopsChannelMapWSErrors.txt", outputLines);
                throw ex;
            }
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    Debug.WriteLine("UnitOfWork is being disposed");
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}