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
    /// <summary> Describes a queen set which is in process of completion. </summary>
    public struct Node
    {
        /// <summary> A column of bitboard-arrays, each representing an attack mask per single rank. </summary>
        public readonly ulong[][] FillColumn;

        /// <summary> A matrix containing occupancy-masks (the squares occupied by the queens). </summary>
        public readonly ulong[] Occupancy;

        /// <summary> Creates a new node with specified parameters. </summary>
        /// <param name="fillColumn"> A fill-column containing already known attack-masks. </param>
        /// <param name="occupancy"> An array of already known bit-positions. </param>
        public Node(ulong[][] fillColumn, ulong[] occupancy)
        {
            FillColumn = fillColumn;
            Occupancy = occupancy;
        }
    }
}