namespace UIInfoSuite2.Infrastucture.Extensions
{
    static class StringExtensions
    {
        public static int SafeParseInt32(this string s)
        {
            int result = 0;

            if (!string.IsNullOrWhiteSpace(s))
            {
                int.TryParse(s, out result);
            }

            return result;
        }

        public static int SafeParseInt64(this string s)
        {
            int result = 0;

            if (!string.IsNullOrWhiteSpace(s))
                int.TryParse(s, out result);

            return result;
        }

        public static bool SafeParseBool(this string s)
        {
            bool result = false;

            if (!string.IsNullOrWhiteSpace(s))
            {
                bool.TryParse(s, out result);
            }

            return result;
        }
    }
}
