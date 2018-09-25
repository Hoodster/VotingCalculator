using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalkulator_Wyborczy.Services
{
    class Connection
    {
        public Connection()
        {

        }
        public async Task<FirebaseClient> GetFirebaseClient()
        {
            return await Task.FromResult(new FirebaseClient("https://votingcalculator.firebaseio.com"));
        }
    }
}
