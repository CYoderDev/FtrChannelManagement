using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
