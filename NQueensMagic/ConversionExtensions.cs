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

using System.Runtime.CompilerServices;
using System.Text;

namespace NQueensMagic
{
    /// <summary> A collection of extension methods that convert between different
    /// representations of a queen set: bitboards, arrays, strings. </summary>
    public static class ConversionExtensions
    {
        /// <summary> Converts a matrix containing a full set of queens to an array of Y-coordinates. </summary>
        /// <param name="mtx"> A matrix (array of bitboards) to be converted. </param>
        /// <returns> An array of Y-coordinates. </returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] ToYCoordinates(this ulong[] mtx)
        {
            // IMPORTANT: It's assumed that the set is full, i.e. the number of queens = N.

            var yCoordinates = new int[Master.N];

            // 'i' represents the file (column) index.
            for (var i = 0; i < Master.N; i++)
            {
                // Get the bitboard's 2D components:
                int bbIdx_X = i / 8;
                int bbIdx_Y = 0;

                // Isolate the bit-position using the appropriate bit-file mask:
                ulong bitFile = BitBase.BitFiles[i % 8];
                ulong bitPos = mtx[bbIdx_X] & bitFile;

                /* The following loop will cause an error if the set is not complete.
                   The reason for this is simple: the loop iterates through each
                   bitboard vertically until a queen is found. If the file isn't
                   occupied then Y will go out of range. And we don't perform
                   any checks because: A) performance, B) this method is not supposed
                   to return any results if the set isn't complete. */

                while (bitPos == 0)
                    bitPos = mtx[bbIdx_X + (++bbIdx_Y) * Board.Dimension] & bitFile;

                yCoordinates[i] = BitBase.BitToY[bitPos] + bbIdx_Y * 8;
            }

            return yCoordinates;
        }

        /// <summary> Converts a matrix to a string of Y-coordinates. </summary>
        /// <param name="mtx"> A matrix (array of bitboards) to be converted. </param>
        /// <returns> A string of Y-coordinates. </returns>
        /// <remarks> The queen set doesn't necessarily have to be full. It can be a preset. </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToQueenString(this ulong[] mtx)
        {
            var sb = new StringBuilder();

            /* This method is similar to 'ToYCoordinates' except the while loop does perform the 'out-of-range'
               check because now the set is allowed to be incomplete, and empty files are be marked by the skip sign. */

            for (var i = 0; i < Master.N; i++)
            {
                int bbIdx_X = i / 8;
                int bbIdx_Y = 0;

                ulong bitFile = BitBase.BitFiles[i % 8];
                ulong bitPos = mtx[bbIdx_X] & bitFile;

                while (bitPos == 0)
                {
                    if (++bbIdx_Y == Board.Dimension) break;
                    bitPos = mtx[bbIdx_X + bbIdx_Y * Board.Dimension] & bitFile;
                }

                var line = bitPos > 0 ? (BitBase.BitToY[bitPos] + bbIdx_Y * 8).ToString() : Serializer.SkipSign;
                sb.Append($"{line} ");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary> Attempts to convert an array of Y-coordinates to a queen set. </summary>
        /// <param name="yCoordinates"> An array of Y-coordinates to be converted. </param>
        /// <param name="set"> The result of conversion. </param>
        /// <returns> Are the queens non-attacking in relation to each other? </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToQueenSet(this int[] yCoordinates, out QueenSet set)
        {
            set = QueenSet.Empty();

            // Iterate through each file of the board. They are represented by
            // Y-coordinates that can be converted to a position-attacks pair.

            for (var i = 0; i < Master.N; i++)
            {
                var y = yCoordinates[i];

                // Free files are marked as -1. Skip those.
                if (y == -1) continue;

                // Get the bitboard index and the bit-position.
                var bbIdx = i / 8 + y / 8 * Board.Dimension;
                var bitPos = 1UL << (i % 8 + y % 8 * 8);

                // Evaluates to true if the bit-position is attacked by any of the queens placed so far.
                // In that case there's no need to proceed with the conversion, and we can return false:
                if ((bitPos & set.Fill[bbIdx]) != 0) return false;

                // Populate the occupancy-fill pair:

                set.Occupancy[bbIdx] |= bitPos;
                var attacks = BitBase.Attacks[bbIdx][bitPos];

                for (var j = 0; j < Board.BitboardCount; j++)
                    set.Fill[j] |= attacks[j];
            }
            return true;
        }

        /// <summary> Calculates the number of queens comprising an array of Y-coordinates. </summary>
        /// <param name="arr"> An array of Y-coordinates. </param>
        /// <returns> The number of queens found. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NumberOfQueens(this int[] arr)
        {
            var size = 0;

            for (var i = 0; i < Master.N; i++)
            {
                // Free files are marked as -1. Skip those.
                if (arr[i] >= 0) size++;
            }
            return size;
        }
    }
}
