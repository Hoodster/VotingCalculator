using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalkulator_Wyborczy.Data
{
    class DatabaseHelper
    {
       
        public List<Candidate> Candidates { get; set; }
        public List<Voter> Voters { get; set; }
        public Votes Votes { get; set; }
    }
}
