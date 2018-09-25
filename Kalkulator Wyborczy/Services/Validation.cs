using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Kalkulator_Wyborczy.Services
{
    class Validation
    {
        Cryptography cryptography;
        public Validation()
        {
            cryptography = new Cryptography();
        }
        private readonly int[] checks = { 9, 7, 3, 1, 9, 7, 3, 1, 9, 7 };

        #region PESEL
        /// <summary>
        /// Checks all PESEL validations.
        /// </summary>
        /// <param name="pesel">PESEL number</param>
        /// <returns></returns>
        public Task<bool> checkPESEL(string pesel)
        {
            bool result = false;
            if (pesel.Length == 11)
            {
                result = checkPESELSum(pesel).Equals(pesel[10].ToString());
                return Task.FromResult(result);
            }
            return Task.FromResult(result);
        }

        /// <summary>
        /// Check if PESEL fits mulitplier validation.
        /// </summary>
        /// <param name="pesel">PESEL number</param>
        /// <returns></returns>
        private string checkPESELSum(string pesel)
        {
            int sum = 0;
            for (int i = 0; i < checks.Length; i++)
            {
                sum += checks[i] * int.Parse(pesel[i].ToString());
            }
            return (sum % 10).ToString();
        }
        #endregion

        #region Maturity
        /// <summary>
        /// Check if user is at least 18.
        /// </summary>
        /// <param name="pesel">PESEL number</param>
        /// <returns></returns>
        public async Task<bool> CheckIfMatureAsync(string pesel)
        {
            int year, day;
            //month
            int rightmonth = int.Parse(pesel.Substring(2, 2));
            //year
            string century = "", controlmonth = pesel.Substring(2, 1);
            //count century according to PESEL ISO, valid to maximum PESEL year - 2299
            switch (controlmonth)
            {
                case "8":
                case "9":
                    century = "18";
                    rightmonth -= 80;
                    break;
                case "0":
                case "1":
                    century = "19";
                    break;
                case "2":
                case "3":
                    century = "20";
                    rightmonth -= 20;
                    break;
                case "4":
                case "5":
                    century = "21";
                    rightmonth -= 40;
                    break;
                case "6":
                case "7":
                    century = "22";
                    rightmonth -= 60;
                    break;
            }
            year = int.Parse(century + pesel.Substring(0, 2));
            //day
            day = int.Parse(pesel.Substring(4, 2));
            //check is user got up to 18
            DateTime now = DateTime.Now;
            DateTime userage = new DateTime(year, rightmonth, day);
            DateTime uptoeighteen = now.AddYears(-18);
            if (userage <= uptoeighteen)
                return await Task.FromResult(true);
            else
            {
                MessageBox.Show("You are juvenile. You can't vote.");
                return await Task.FromResult(false);
            }
        }

        /// <summary>
        /// Check if user has voting rights.
        /// </summary>
        /// <param name="pesel">PESEL number</param>
        /// <returns>Boolean.</returns>
        public async Task<bool> CheckIfAllowedAsync(string pesel)
        {

            //gets blacklist JSON and compares user's PESEL with PESEL numbers from blacklist
            string sStream;
            var sPath = "http://webtask.future-processing.com:8069/blocked";
            using (var WebClient = new WebClient())
            {
                sStream = WebClient.DownloadString(sPath);
            }
            JObject disallowed = JObject.Parse(sStream);
            IList<JToken> results = disallowed["disallowed"]["person"].Children().ToList();

            foreach (JToken p in results)
            {
                string s = p.ToObject<Person>().PESEL;
                //if any PESEL matches user is not allowed, app sends notification and returns false
                if (pesel.Equals(s))
                {             
                    MessageBox.Show("You don't have voting rights.");
                    return await Task.FromResult(false);
                }
            }
            return await Task.FromResult(true);
        }
        #endregion
    }
}
