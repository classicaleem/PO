using System;

namespace HRPackage.Helpers
{
    public static class NumberToWords
    {
        public static string ConvertAmount(double amount)
        {
            try
            {
                long amount_int = (long)amount;
                long amount_dec = (long)Math.Round((amount - (double)(amount_int)) * 100);
                if (amount_dec == 0)
                {
                    return Convert(amount_int) + " Only";
                }
                else
                {
                    return Convert(amount_int) + " Point " + Convert(amount_dec) + " Only";
                }
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public static string Convert(long i)
        {
            if (i < 20)
            {
                return Units(i);
            }
            if (i < 100)
            {
                return Tens(i);
            }
            if (i < 1000)
            {
                return Units(i / 100) + " Hundred " + Convert(i % 100);
            }
            if (i < 100000)
            {
                return Convert(i / 1000) + " Thousand " + Convert(i % 1000);
            }
            if (i < 10000000)
            {
                return Convert(i / 100000) + " Lakh " + Convert(i % 100000);
            }
            return Convert(i / 10000000) + " Crore " + Convert(i % 10000000);
        }

        private static string Units(long i)
        {
            string[] units = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            return units[i];
        }

        private static string Tens(long i)
        {
            string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
            return tens[i / 10] + ((i % 10 > 0) ? " " + Units(i % 10) : "");
        }
    }
}
