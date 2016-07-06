using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace FrontierVOps.ChannelMapWS.Models
{
    public class Logo
    {
        public byte[] Image { get; set; }

        public static Bitmap ConvertToBitmap(byte[] ImgBytes)
        {
            Bitmap bmp;
            using (var ms = new MemoryStream(ImgBytes))
            {
                bmp = new Bitmap(ms);
            }
            return bmp;
        }

        public static bool CompareBitmaps(Bitmap firstImage, Bitmap secondImage)
        {
            using (var ms = new MemoryStream())
            {
                firstImage.Save(ms, ImageFormat.Png);
                string firstBitmap = Convert.ToBase64String(ms.ToArray());
                ms.Position = 0;

                secondImage.Save(ms, ImageFormat.Png);
                string secondBitmap = Convert.ToBase64String(ms.ToArray());

                return firstBitmap.Equals(secondBitmap);
            }
        }

        public static byte[] ConvertToBytes(Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }
}