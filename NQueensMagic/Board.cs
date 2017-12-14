/*
    NQueensMagic, a program that solves the N queens puzzle
    Copyright (C) 2017 Aaron Alfer
    
    NQueensMagic is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    NQueensMagic is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace NQueensMagic
{
    /// <summary>
    /// Contains the board's attributes: dimension, bitboard indices, etc.
    /// </summary>
    public static class Board
    {
        /// <summary> The number of bitboards along one side of the matrix. </summary>
        public static int Dimension { get; private set; }

        /// <summary> The total number of bitboards in the matrix. </summary>
        public static int BitboardCount { get; private set; }

        /// <summary> The last bitboard's index. </summary>
        public static int LastBitboardIndex { get; private set; }

        /// <summary> Index of the first bitboard in the last row. </summary>
        public static int LastRightmostBitboardIndex { get; private set; }

        /// <summary> The last rank number. </summary>
        public static int LastRank { get; private set; }

        /// <summary> The bitboard representation of the last rank. </summary>
        public static ulong LastBitRank { get; private set; }

        /// <summary> Updates the board's dimensions and other attributes. </summary>
        public static void Reset(int remainder)
        {
            ResetDimension(remainder);
            BitboardCount = Dimension * Dimension;
            LastBitboardIndex = BitboardCount - 1;
            LastRightmostBitboardIndex = BitboardCount - Dimension;

            LastRank = Master.N - 1;
            LastBitRank = BitBase.BitRanks[LastRank % 8];
        }

        /// <summary>
        /// Resets the dimension, i.e. the number of bitboards along one side of the matrix.
        /// </summary>
        /// <param name="remainder"> The number of trailing files/ranks. </param>
        private static void ResetDimension(int remainder)
        {
            // Calculate an excessive N: the smallest power of 8 bigger than N:
            var nExcessive = Master.N + 8 - remainder;
            Dimension = nExcessive / 8;

            /* By using the excessive N instead of just N we make sure that the trailing files and ranks
               are included. For instance, in case of a 9x9 board, if we just divide 9 by 8 we get only
               1 bitboard - which is not enough to hold all of the files/ranks. What we should do instead,
               is to divide 16 by 8 and get 4 bitboards in total (2x2) so that the last file and rank are included. */
        }
    }
}
