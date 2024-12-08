using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sly_Scorpion
{
    internal class Zobrist
    {
        public static ulong[,] zobristKeys = new ulong[12, 64];  // 12 piece types (6 for each color), 64 squares

        public static void InitializeZobristKeys()
        {
            Random rng = new Random(123456789);  // Seed for reproducibility
            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    zobristKeys[i, j] = GetRandomUlong(rng);
                }
            }
        }

        private static ulong GetRandomUlong(Random rng)
        {
            byte[] buf = new byte[8];
            rng.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }
        public static ulong CalculateZobristHash(ulong[] white, ulong[] black)  // Array of bitboards for each piece type
        {
            ulong hash = 0;
            for (int pieceType = 0; pieceType < 12; pieceType++)  // Assuming 12 types, 6 for each color
            {
                ulong bitboard;
                if (pieceType < 6)
                    bitboard = white[pieceType];
                else
                    bitboard = black[pieceType-6];

                while (bitboard != 0)
                {
                    int square = BitScanForward(bitboard);
                    hash ^= zobristKeys[pieceType, square];
                    bitboard &= bitboard - 1;  // Clear the least significant bit set
                }
            }
            return hash;
        }

        private static int BitScanForward(ulong bb)
        {
            return System.Numerics.BitOperations.TrailingZeroCount(bb);
        }
    }
}
