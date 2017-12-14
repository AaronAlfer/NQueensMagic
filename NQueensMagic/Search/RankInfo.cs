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

namespace NQueensMagic.Search
{
    /// <summary> Contains some information associated with a board-rank. </summary>
    public struct RankInfo
    {
        /// <summary> Index of the first bitboard in the row containing the rank. </summary>
        public readonly int BitboardIndex;

        /// <summary> The bitboard representation of the rank. </summary>
        public readonly ulong BitRank;

        /// <summary>
        /// Calculates the parameters associated with a board-rank, and stores them.
        /// </summary>
        /// <param name="rank"> A board-rank. </param>
        public RankInfo(int rank)
        {
            BitboardIndex = rank / 8 * Board.Dimension;
            BitRank = BitBase.BitRanks[rank % 8];
        }
    }
}