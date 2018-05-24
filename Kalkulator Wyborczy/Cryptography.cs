using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalkulator_Wyborczy
{
    class Cryptography
    {
        public static string Encode(string input)
        {
            byte[] inputArray = UTF8Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(inputArray);
        }

        public static string Decode(string input)
        {
            byte[] inputArray = Convert.FromBase64String(input);
            return UTF8Encoding.UTF8.GetString(inputArray);
        }
    }
}
