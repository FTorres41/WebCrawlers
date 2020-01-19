using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Util
{
    class DateHandle
    {

        public static DateTime? Parse(string date, string format)
        {
            try { 
                DateTime dt;
                DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                dt = dt == DateTime.MinValue ? DateTime.Parse(date) : dt;
                return dt;
            }
            catch (Exception e)
            {
                RService.Log("RService Error on: {0} " + e.Message + e.StackTrace, Path.GetTempPath() + "RSERVICE" + ".txt");
                return null;
            }
        }

    }
}
