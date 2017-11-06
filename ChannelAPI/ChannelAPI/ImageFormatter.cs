using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace ChannelAPI
{
    public class ImageFormatter : InputFormatter
    {
        public ImageFormatter()
        {
            SupportedMediaTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("image/png"));
            SupportedMediaTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream"));
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var img = Image.FromStream(context.HttpContext.Request.Body);
            return await InputFormatterResult.SuccessAsync(img);
        }
    }
}
