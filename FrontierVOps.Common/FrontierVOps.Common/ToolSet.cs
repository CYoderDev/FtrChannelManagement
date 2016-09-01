using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.Common
{
    public class Toolset
    {

        #region SecureString
        /// <summary>
        /// Safely converts a secure string type to a string type
        /// </summary>
        /// <param name="Value">Secure string to convert</param>
        /// <returns>SecureString as string</returns>
        public static string ConvertToInsecureString(SecureString Value)
        {
            if (Value == null)
                throw new ArgumentNullException("Value cannot be null");

            IntPtr unmanagedStr = IntPtr.Zero;

            try
            {
                unmanagedStr = Marshal.SecureStringToGlobalAllocUnicode(Value);
                return Marshal.PtrToStringUni(unmanagedStr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedStr);
            }
        }

        /// <summary>
        /// Creates a read-only secure string from an unsecure string
        /// </summary>
        /// <param name="Value">String to convert to SecureString</param>
        /// <returns>A read-only secure string</returns>
        public static SecureString CreateSecureString(string Value)
        {
            char[] strCharArr = Value.ToCharArray();
            SecureString strSecure = new SecureString();

            for (int i = 0; i < strCharArr.Length; i++)
            {
                strSecure.AppendChar(strCharArr[i]);
            }

            strSecure.MakeReadOnly();
            
            return strSecure;
        }
        #endregion //SecureString

        #region GPS / DateTime Conversion
        /// <summary>
        /// Converts a date to GPS time (number of seconds from 1/6/1980 00:00:00)
        /// </summary>
        /// <param name="Date">Date to convert to GPS</param>
        /// <returns>GPS time as double</returns>
        public static double ConvertDateToGPS(DateTime Date)
        {
            return (Date - new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        /// <summary>
        /// Converts GPS (number of seconds from 1/6/1980 00:00:00) to UTC DateTime
        /// </summary>
        /// <param name="GPS">GPS time</param>
        /// <returns>UTC DateTime</returns>
        public static DateTime ConvertGPStoDate(object GPS)
        {
            return ConvertGPStoDate(Convert.ToDouble(GPS));
        }

        /// <summary>
        /// Converts GPS (number of seconds from 1/6/1980 00:00:00) to UTC DateTime
        /// </summary>
        /// <param name="GPS">GPS time as a double-precision floating-point number</param>
        /// <returns>UTC DateTime</returns>
        public static DateTime ConvertGPStoDate(double GPS)
        {
            return new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc).AddSeconds(GPS).ToUniversalTime();
        }
        #endregion //GPS - DateTime Conversion

        #region Epoch / DateTime Conversion
        /// <summary>
        /// Converts a UTC date/time to a UNIX/Epoch date
        /// (number of seconds from 1/1/1970 00:00:00)
        /// </summary>
        /// <param name="Date">UTC DateTime</param>
        /// <returns>UNIX/Epoch date as type double</returns>
        public static double ConvertDateToEpoch(DateTime Date)
        {
            return (Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        /// <summary>
        /// Converts a Unix/Epoch date to a UTC DateTime
        /// </summary>
        /// <param name="Epoch">number of seconds from 1/1/1970 00:00:00</param>
        /// <returns>UTC DateTime</returns>
        public static DateTime ConvertEpochToDate(object Epoch)
        {
            return ConvertEpochToDate(Convert.ToDouble(Epoch));
        }

        /// <summary>
        /// Converts a Unix/Epoch date to a UTC DateTime
        /// </summary>
        /// <param name="Epoch">number of seconds from 1/1/1970 00:00:00</param>
        /// <returns>UTC DateTime</returns>
        public static DateTime ConvertEpochToDate(double Epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Epoch).ToUniversalTime();
        }
        #endregion //Epoch-DateTime Conversion

        #region Bitmaps 
        public static bool CompareBitmaps(Bitmap firstImage, Bitmap secondImage)
        {
            return (Toolset.CompareBitmaps(Toolset.ConvertToBytes(firstImage), Toolset.ConvertToBytes(secondImage)));
        }

        public static bool CompareBitmaps(byte[] firstImage, byte[] secondImage)
        {
            return firstImage.SequenceEqual(secondImage);
        }

        public static bool CompareBitmaps(string firstImage, Bitmap secondImage)
        {
            using (var ms = new MemoryStream())
            {
                secondImage.Save(ms, ImageFormat.Png);
                string secondBitmap = Convert.ToBase64String(ms.ToArray());

                return firstImage.Equals(secondBitmap);
            }
        }

        public static bool CompareBitmaps(string firstPath, string secondPath)
        {
            using (var bm1 = new Bitmap(firstPath))
            using (var bm2 = new Bitmap(secondPath))
            {
                return CompareBitmaps(bm1, bm2);
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

        public static Bitmap ConvertToBitmap(byte[] ImgBytes)
        {
            Bitmap bmp;
            using (var ms = new MemoryStream(ImgBytes))
            {
                bmp = new Bitmap(ms);
            }
            return bmp;
        }

        public static Bitmap ResizeBitmap(string FilePath, int ? NewWidthPx, int? NewHeightPx, float? DPIWidth, float? DPIHeight, bool FixedSize = false)
        {
            if (File.Exists(FilePath))
            {
                return ResizeBitmap(new Bitmap(FilePath), NewWidthPx, NewHeightPx, DPIWidth, DPIHeight, FixedSize);
            }
            else
                throw new FileNotFoundException("Could not find " + FilePath);
        }

        public static Bitmap ResizeBitmap(Bitmap OriginalBM, int? NewWidthPx, int? NewHeightPx, float? DPIWidth, float? DPIHeight, bool FixedSize = false)
        {
            if ((NewHeightPx.HasValue && OriginalBM.Height == NewHeightPx.Value)
                && (NewWidthPx.HasValue && OriginalBM.Width == NewWidthPx.Value)
                && !(DPIHeight.HasValue && DPIWidth.HasValue))
            {
                return OriginalBM;
            }

            int oldWidth = OriginalBM.Width;
            int oldHeight = OriginalBM.Height;

            //if a dimenion is null or 0, set it to the original image dimension
            if ((!NewWidthPx.HasValue) || NewWidthPx.Value == 0)
                NewWidthPx = oldWidth;
            if ((!NewHeightPx.HasValue) || NewHeightPx.Value == 0)
                NewHeightPx = oldHeight;

            //create placeholders for the new dimensions
            int tempWidth = 0;
            int tempHeight = 0;

            if (FixedSize)
            {
                tempWidth = NewWidthPx.Value;
                tempHeight = NewHeightPx.Value;
            }
            else
            {
                //PORTRAIT STYLE
                if (oldWidth < oldHeight)
                {
                    tempWidth = NewWidthPx.Value;
                    //calculate new height based on max width
                    tempHeight = (int)Math.Round((tempWidth * oldHeight) / (double)oldWidth);

                    //if the temp height calculation is larger than the specified max height, recalculate the width
                    if (tempHeight > NewHeightPx.Value)
                    {
                        tempHeight = NewHeightPx.Value;
                        tempWidth = (int)Math.Round((tempHeight * oldWidth) / (double)oldHeight);
                    }
                }
                //LANDSCAPE STYLE
                else
                {
                    tempHeight = NewHeightPx.Value;
                    //calculate new width based on max height
                    tempWidth = (int)Math.Round((tempHeight * oldWidth) / (double)oldHeight);

                    //if the temp width calculation is larger than the specified max width, recalculate the height
                    if (tempWidth > NewWidthPx.Value)
                    {
                        tempWidth = NewWidthPx.Value;
                        tempHeight = (int)Math.Round((tempWidth * oldHeight) / (double)oldWidth);
                    }
                }
            }

            //create new bitmap with new dimensions
            Bitmap newBitmap = new Bitmap(tempWidth, tempHeight);

            //re-draw image from the provided image
            using (Graphics graphic = Graphics.FromImage(newBitmap))
            {
                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                graphic.DrawImage(OriginalBM, 0, 0, tempWidth, tempHeight);
            }

            //Set DPI resolution values if provided
            if (DPIWidth.HasValue && DPIHeight.HasValue)
                newBitmap.SetResolution((float)DPIWidth.Value, (float)DPIHeight.Value);
            else if (DPIWidth.HasValue)
                newBitmap.SetResolution((float)DPIWidth.Value, newBitmap.VerticalResolution);
            else if (DPIHeight.HasValue)
                newBitmap.SetResolution(newBitmap.HorizontalResolution, (float)DPIHeight.Value);

            OriginalBM.Dispose();

            return newBitmap;
        }
        #endregion Bitmaps

        #region Email
        /// <summary>
        /// Sends an email
        /// </summary>
        /// <param name="smtpServer">SMTP email server name or ip</param>
        /// <param name="credentials">SMTP credentials [optional]</param>
        /// <param name="port">SMTP port to send on [optional]</param>
        /// <param name="useSSL">Use SSL?</param>
        /// <param name="sendSubject">Email Subject</param>
        /// <param name="sendBody">Email Body/Message (html)</param>
        /// <param name="sendFrom">Email address that is sending the email</param>
        /// <param name="sendTo">Email addresses to send to (comma delimited)</param>
        /// <param name="attachments">Full path to file attachments</param>
        public static void SendEmail(string smtpServer, NetworkCredential credentials, int? port, bool useSSL, bool isHTML, string sendSubject, string sendBody, string sendFrom, string[] sendTo, string[] attachments)
        {
            MailMessage mail = new MailMessage();
            SmtpClient smtp = new SmtpClient(smtpServer);
            mail.From = new MailAddress(sendFrom);
            mail.Subject = sendSubject;
            mail.IsBodyHtml = isHTML;
            mail.Body = sendBody;

            for (int i = 0; i < sendTo.Length; i++ )
            {
                mail.To.Add(sendTo[i]);
            }

            for (int i = 0; i < attachments.Length; i++)
            {
                Attachment attachment = new Attachment(attachments[i]);
                mail.Attachments.Add(attachment);
            }

            if (useSSL && credentials == null)
                throw new ArgumentException("Must provide credentials if using SSL.", "credentials");

            smtp.Port = port.HasValue ? port.Value : 25;
            smtp.Credentials = credentials;
            smtp.EnableSsl = useSSL;

            smtp.Send(mail);
        }
        #endregion
    }
}
