using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Move_Generation
{
    internal class Move
    {
        public ulong start;
        public ulong end;

        public Move(ulong start, ulong end)
        {
            this.start = start;
            this.end = end;
        }

        public string ToString()
        {
            int p1 = (int)Math.Log2(start);
            int p2 = (int)Math.Log2(end);

            // Calculates the row and column of the piece
            int r1 = p1 / 8;
            int c1 = p1 % 8;

            int r2 = p2 / 8;
            int c2 = p2 % 8;
            return "(" + c1 + ", " + r1 + ") to (" + c2 + ", " + r2 + ") : (" + start + ", " + end + ")";
        }
    }
}
