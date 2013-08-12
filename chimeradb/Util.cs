using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections;
using System.Reflection;

namespace genericdbgenerator
{
    /// <summary>
    /// Class for random assorted functionality
    /// </summary>
    public class Util
    {
        public static string GenerateRandomString(int size)
        {
            Random random = new Random((int)DateTime.Now.Ticks);

            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Functie om invalid XML characters te filteren
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public static bool IsLegalXmlChar(int character)
        {
            return
            (
                 character == 0x9 /* == '\t' == 9   */          ||
                 character == 0xA /* == '\n' == 10  */          ||
                 character == 0xD /* == '\r' == 13  */          ||
                (character >= 0x20 && character <= 0xD7FF) ||
                (character >= 0xE000 && character <= 0xFFFD) ||
                (character >= 0x10000 && character <= 0x10FFFF)
            );
        }

        //functie om een lijst in een datatable om te zetten, nodig vanwege SqlBulkCopy.WriteToServer()
        public static DataTable ObtainDataTableFromIEnumerable(IEnumerable ien)
        {
            DataTable dt = new DataTable();
            var ienlist = ien as IList;

            for (int i = 0; i < ienlist.Count; i++)
            {
                Type t = ienlist[i].GetType();
                PropertyInfo[] pis = t.GetProperties();
                if (dt.Columns.Count == 0)
                {
                    foreach (PropertyInfo pi in pis)
                    {
                        dt.Columns.Add(pi.Name, pi.PropertyType);
                    }
                }
                DataRow dr = dt.NewRow();
                foreach (PropertyInfo pi in pis)
                {
                    object value = pi.GetValue(ienlist[i], null);
                    dr[pi.Name] = value;
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }
    }
}
