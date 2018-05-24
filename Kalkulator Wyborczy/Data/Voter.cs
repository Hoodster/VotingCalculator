using Kalkulator_Wyborczy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalkulator_Wyborczy
{
    class Voter
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string PESEL { get; set; }
        public string Password { get; set; }
        public string VotedCandidate { get; set; }
        public bool HasVoted { get; set; }
        public bool ValidVote { get; set; }
  
        public Voter()
        {

        }

        public Voter(string name, string surname, string pesel, string password)
        {
            this.Name = name;
            this.Surname = surname;
            this.PESEL = pesel;
            this.Password = password;
        }
    }
}
