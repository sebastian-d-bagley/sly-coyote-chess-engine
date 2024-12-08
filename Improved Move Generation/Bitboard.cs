using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Move_Generation
{
    internal class Bitboard
    {
        private static readonly ulong[] bitIndexToBitboard = new ulong[64];

        static Bitboard()
        {
            for (int i = 0; i < 64; i++)
            {
                bitIndexToBitboard[i] = 1UL << i;
            }
        }

        public static List<Move> SplitBitboard(ulong start, ulong bitboard)
        {
            List<Move> singleBitBoards = new List<Move>();
            while (bitboard != 0)
            {
                int index = BitOperations.TrailingZeroCount(bitboard);
                ulong ls1b = bitIndexToBitboard[index];
                singleBitBoards.Add(new Move(start, ls1b));
                if (start == 0)
                    throw new Exception();
                bitboard &= bitboard - 1;
            }
            return singleBitBoards;
        }


        // Get all the bits to the left of the position in the same row
        public static ulong GetLeftBits(ulong positionBitboard, ulong sourceBitboard)
        {
            int position = BitOperations.TrailingZeroCount(positionBitboard);
            ulong mask = (1UL << (position % 8)) - 1;  // Mask for the row, up to the position
            mask = mask << (position / 8 * 8);  // Shift mask to the correct row
            return sourceBitboard & mask;
        }

        // Get all the bits to the right of the position in the same row
        public static ulong GetRightBits(ulong positionBitboard, ulong sourceBitboard)
        {
            int pos = BitOperations.TrailingZeroCount(positionBitboard);
            ulong mask = (1UL << pos) - 1;
            ulong bottomLeftCross = sourceBitboard & mask;
            ulong topRightCross = (bottomLeftCross ^ sourceBitboard) ^ positionBitboard;
            return (topRightCross & ~GetUpBits(positionBitboard, sourceBitboard)) & ~positionBitboard;
        }

        // Get all the bits above the position in the same column
        public static ulong GetUpBits(ulong positionBitboard, ulong sourceBitboard)
        {
            // Calculate the position of the single set bit
            int bitIndex = BitOperations.Log2(positionBitboard);

            // Calculate the column index (bitIndex % 8 assuming a 8x8 board)
            int columnIndex = bitIndex % 8;

            // Create a mask for all bits above the given bit in the same column
            // (1UL << columnIndex) creates a single bit at the base of the column
            // ~((1UL << (bitIndex + 1)) - 1) creates a mask with all bits above the bitIndex set
            ulong columnMask = (1UL << columnIndex);
            ulong maskAbove = ~((1UL << (bitIndex + 1)) - 1);

            // Combine the masks to isolate the column and above the position
            ulong fullColumnMask = columnMask * 0x0101010101010101UL; // Extend the single bit into a full column
            ulong resultMask = fullColumnMask & maskAbove;

            // Return the masked sourceBitboard
            return sourceBitboard & resultMask & ~GetDownBits(positionBitboard, sourceBitboard) & ~ positionBitboard;
        }

        // Get all the bits below the position in the same column
        public static ulong GetDownBits(ulong positionBitboard, ulong sourceBitboard)
        {
            int pos = BitOperations.TrailingZeroCount(positionBitboard);
            ulong mask = (1UL << pos) - 1;
            ulong bottomLeftCross = sourceBitboard & mask;
            return bottomLeftCross & ~GetLeftBits(positionBitboard, sourceBitboard);
        }
        // Writes a specific bitboard to the console
        public static void PrintBitboard(ulong bitboard)
        {
            string representation = "O";
            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    ulong position = 1UL << (rank * 8 + file);
                    Debug.Write((bitboard & position) != 0 ? representation + " " : ". ");
                }
                Debug.WriteLine("");
            }
            Debug.WriteLine("");
        }
        public static ulong GetLeastSignificantBit(ulong bitboard)
        {
            // Return 0 if bitboard is 0, as there is no set bit
            if (bitboard == 0) return 0;

            // Get the position of the LSB
            int position = BitOperations.TrailingZeroCount(bitboard);
            return 1UL << position;
        }

        public static ulong GetMostSignificantBit(ulong bitboard)
        {
            // Return 0 if bitboard is 0, as there is no set bit
            if (bitboard == 0) return 0;

            // Get the position of the MSB
            int position = 63 - BitOperations.LeadingZeroCount(bitboard);
            return 1UL << position;
        }
        public static ulong GetMoreSignificantBits(ulong sourceBitboard, ulong positionBitboard)
        {
            // Use bit manipulation to create a mask that has 1s in positions more significant than the found bit
            ulong mask = positionBitboard ^ (positionBitboard - 1);

            // Shift the mask to the right by one to exclude the current 1 bit position and get only more significant bits
            mask = mask >> 1;

            // Apply the mask to the source bitboard to get only more significant bits
            return sourceBitboard & ~mask;
        }
        public static ulong GetLessSignificantBits(ulong sourceBitboard, ulong positionBitboard)
        {
            // Create a mask that has 1s in positions less significant than the found bit
            ulong mask = positionBitboard - 1;

            // Apply the mask to the source bitboard to get only less significant bits
            return sourceBitboard & mask;
        }
    }
}
