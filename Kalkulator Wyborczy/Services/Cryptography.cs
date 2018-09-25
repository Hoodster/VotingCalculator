using System;
using System.Text;
using System.Threading.Tasks;

namespace Kalkulator_Wyborczy.Services
{
    public class Cryptography
    {
        /// <summary>
        /// Encodes string sequence.
        /// </summary>
        /// <param name="input">Text input.</param>
        /// <returns>Base64 string</returns>
        public Task<string> Encode(string input)
        {
            byte[] inputArray = UTF8Encoding.UTF8.GetBytes(input);
            return Task.FromResult(Convert.ToBase64String(inputArray));
        }

        /// <summary>
        /// Decodes encrypted string sequence.
        /// </summary>
        /// <param name="input">Base64 string input.</param>
        /// <returns>Encrypted string.</returns>
        public Task<string> Decode(string input)
        {
            byte[] inputArray = Convert.FromBase64String(input);
            return Task.FromResult(UTF8Encoding.UTF8.GetString(inputArray));
        }
    }
}
