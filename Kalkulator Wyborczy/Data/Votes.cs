using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalkulator_Wyborczy.Data
{
    class Votes
    {
        public int Valid { get; set; }
        public int Invalid { get; set; }

        public int Blocked { get; set; }

        public Votes()
        {

        }

        public Votes(int valid, int invalid, int blocked)
        {
            this.Valid = valid;
            this.Invalid = invalid;
            this.Blocked = blocked;
        }
    }
}
