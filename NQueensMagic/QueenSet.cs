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
    /// <summary> An occupancy-fill pair representing a combination of queens on the board. </summary>
    public struct QueenSet
    {
        /// <summary> A set of bitboards representing all queens' positions. </summary>
        public readonly ulong[] Occupancy;

        /// <summary> A set of bitboards representing a fill mask. </summary>
        /// <remarks> A fill mask works this way: all the squares that are attacked
        /// and therefore cannot be populated are marked as bit 1, whereas all other squares
        /// (free squares) are marked as bit 0. </remarks>
        public readonly ulong[] Fill;

        /// <summary> Creates a new queen set. </summary>
        /// <param name="occupancy"> A set of bitboards representing all queens' positions. </param>
        /// <param name="fill"> A set of bitboards representing a fill mask. </param>
        public QueenSet(ulong[] occupancy, ulong[] fill)
        {
            Occupancy = occupancy;
            Fill = fill;
        }

        /// <summary> Creates an empty queen set with bitboards initialized. </summary>
        public static QueenSet Empty()
        {
            var bbCount = Board.BitboardCount;
            var dim = Board.Dimension;

            var tFilesInv = ~BitBase.TrailingBitFiles;
            var tRanksInv = ~BitBase.TrailingBitRanks;

            var emptySet = new QueenSet(new ulong[bbCount], new ulong[bbCount]);

            // Exclude all files beyond N:
            for (var i = dim - 1; i < bbCount; i += dim)
                emptySet.Fill[i] = tFilesInv;

            // Exclude all ranks beyond N:
            for (var i = bbCount - dim; i < bbCount; i++)
                emptySet.Fill[i] |= tRanksInv;

            return emptySet;
        }
    }
}