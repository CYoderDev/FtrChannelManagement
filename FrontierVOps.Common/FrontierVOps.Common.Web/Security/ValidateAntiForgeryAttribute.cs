using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace FrontierVOps.Common.Web.Security
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, 
        AllowMultiple = false, Inherited = true)]
    public sealed class ValidateAntiForgeryAttribute : FilterAttribute, IAuthorizationFilter
    {
        public Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {
            Task<HttpResponseMessage> ret = null;
            
            try
            {
                // Validating
                ValidateToken(actionContext.Request);

                ret = continuation();
            }
            catch(System.Web.Mvc.HttpAntiForgeryException)
            {
                //403 Forbidden Response
                actionContext.Response = new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.Forbidden,
                    RequestMessage = actionContext.ControllerContext.Request
                };

                var source = new TaskCompletionSource<HttpResponseMessage>();
                source.SetResult(actionContext.Response);

                ret = source.Task;
            }

            return ret;
        }

        private void ValidateToken(HttpRequestMessage request)
        {
            AntiForgery.Validate();
        }
    }

}
