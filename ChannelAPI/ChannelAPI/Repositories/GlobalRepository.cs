using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace ChannelAPI.Repositories
{
    public class GlobalRepository
    {
        internal static string GetBoundary(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));

            var elements = contentType.Split(' ');
            var element = elements.First(entry => entry.StartsWith("boundary="));
            var boundary = new Microsoft.Extensions.Primitives.StringSegment(element.Substring("boundary=".Length));
            
            boundary = HeaderUtilities.RemoveQuotes(boundary);

            return boundary.ToString();
        }
    }
}
