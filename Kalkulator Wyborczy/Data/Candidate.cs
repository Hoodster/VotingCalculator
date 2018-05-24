using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalkulator_Wyborczy.Data
{
    class Candidate
    {
        public string name { get; set; }
        public string party { get; set; }
        public int votes { get; set; }

        public Candidate()
        {
        }

       

        public Candidate(string name, string party, int votes)
        {
            this.name = name;
            this.party = party;
            this.votes = votes;
        }
    }
}
