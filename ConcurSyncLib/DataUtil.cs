using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcurSyncLib
{
    public class DataUtil
    {

        public static String ToSqlString(String val)
        {
            if (val == null )
            {
                return "''";
            }
            else
            {
                return String.Format("'{0}'", val.Replace("'", "''"));
            }
            

        }

        public static Decimal GetDecimal(Object val)
        {
            if (val == null)
            {

                return 0;
            }
            else if (val == System.DBNull.Value)
            {
                return 0;

            }
            else if (val == "")
            {
                return 0;
            }
            else
            {

                return Convert.ToDecimal(val);
            }
        }

        public static String GetString(Object val)
        {
            if (val == null)
            {
                return "";

            }
            else if (val == System.DBNull.Value)
            {
                return "";
            }
            else
            {

                return Convert.ToString(val).Trim();
            }


        }

        public static Double GetDouble(Object val)
        {
            if (val == null)
            {

                return 0;
            }
            else if (val == System.DBNull.Value)
            {
                return 0;

            }
            else if (val == "")
            {
                return 0;
            }
            else
            {

                return Convert.ToDouble(val);
            }
        }

        public static DateTime GetDate(object Value)
        {
            if (Value == null)
                return new DateTime();
            else if (Convert.ToString(Value).Trim().Length == 0)
                return new DateTime();
            else
                return Convert.ToDateTime(DateTime.FromOADate(Convert.ToDouble(Value)));
        }

        public static string ToSqlDate(DateTime dt)
        {
            if (dt.Year == 1)
            {
                return "''";
            }
            else
            {
                return String.Format("convert(datetime, '{0}', 101)", dt.ToString("MM/dd/yyyy"));
            }
            
        }

    }
}
