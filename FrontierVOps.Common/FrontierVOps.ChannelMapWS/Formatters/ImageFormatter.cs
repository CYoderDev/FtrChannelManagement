using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web;

namespace FrontierVOps.ChannelMapWS.Formatters
{
    public class ImageFormatter : BufferedMediaTypeFormatter
    {
        public ImageFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("image/png"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/octet-stream"));
        }

        public override bool CanWriteType(Type type)
        {
            if (type == typeof(Bitmap))
                return true;
            else
            {
                Type enumType = typeof(IEnumerable<Image>);
                return enumType.IsAssignableFrom(type);
            }
        }

        public override bool CanReadType(Type type)
        {
            return type == typeof(Bitmap);
        }

        public override void WriteToStream(Type type, object value, Stream writeStream, HttpContent content)
        {

            var images = value as IEnumerable<Bitmap>;
            if (images != null)
            {
                foreach (var img in images)
                {
                    WriteItem(img, writeStream);
                }
            }
            else
            {
                var singleImg = value as Bitmap;
                if (singleImg == null)
                    throw new InvalidOperationException("Cannot serialize type");
                WriteItem(singleImg, writeStream);
            }
        }

        private void WriteItem(Bitmap img, Stream writer)
        {
            writer.Position = 0;
            img.Save(writer, System.Drawing.Imaging.ImageFormat.Png);
        }

        public override object ReadFromStream(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            var img = new Bitmap(readStream);
            return img;
            
            //return base.ReadFromStream(type, readStream, content, formatterLogger);
        }
    }
}