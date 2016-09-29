using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace FrontierVOps.Common.Web.ActionResults
{
    public class FileResult : IHttpActionResult
    {
        public string FilePath { get; private set; }
        public string ContentType { get; private set; }

        public FileResult(string filePath, string contentType = null)
        {
            if (File.Exists(filePath))
                this.FilePath = filePath;
            else
                throw new FileNotFoundException("Could not locate file.", filePath);

            this.ContentType = contentType;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StreamContent(File.OpenRead(this.FilePath));
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
            this.ContentType = this.ContentType ?? MimeMapping.GetMimeMapping(Path.GetExtension(FilePath));
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(this.ContentType);
            return Task.FromResult(response);
        }
    }
}
