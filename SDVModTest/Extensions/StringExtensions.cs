using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite.Extensions
{
    static class StringExtensions
    {


        public static Int32 SafeParseInt32(this String s)
        {
            Int32 result = 0;

            if (!String.IsNullOrWhiteSpace(s))
            {
                Int32.TryParse(s, out result);
            }

            return result;
        }

        public static bool SafeParseBool(this String s)
        {
            bool result = false;

            if (!String.IsNullOrWhiteSpace(s))
            {
                Boolean.TryParse(s, out result);
            }

            return result;
        }
    }
}
