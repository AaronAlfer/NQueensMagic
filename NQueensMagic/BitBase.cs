using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NQueensMagic
{
    /// <summary> Contains various bitmasks and methods for precalculation. </summary>
    public static class BitBase
    {
        #region Properties

        /// <summary> A bitmask that excludes all bit-files beyond N. </summary>
        public static ulong TrailingBitFiles { get; private set; }

        /// <summary> A bitmask that excludes all bit-ranks beyond N. </summary>
        public static ulong TrailingBitRanks { get; private set; }

        /// <summary> The rightmost bitboards' indices, each corresponding to a given rank. </summary>
        public static int[] RightmostBitboardIndices { get; private set; }

        /// <summary> Contains every attack mask per bit-position. </summary>
        /// <remarks> We use an array of dictionaries because each dictionary corresponds to a single bitboard. </remarks>
        public static Dictionary<ulong, ulong[]>[] Attacks { get; private set; }

        /// <summary> A conversion table that can be used to convert a bit-position to a Y-coordinate. </summary>
        public static Dictionary<ulong, int> BitToY { get; private set; }

        /// <summary> A conversion table that can be used to convert a bit-position to an X-coordinate. </summary>
        public static Dictionary<ulong, int> BitToX { get; private set; }

        /// <summary> The time it took the last precalculation to complete. </summary>
        public static TimeSpan Elapsed { get; private set; }

        /// <summary> Bitmasks representing the 8 files on a single 8x8 bitboard. </summary>
        public static IReadOnlyList<ulong> BitFiles { get; } = new ulong[]
        {
            0x101010101010101,
            0x202020202020202,
            0x404040404040404,
            0x808080808080808,
            0x1010101010101010,
            0x2020202020202020,
            0x4040404040404040,
            0x8080808080808080
        };

        /// <summary> Bitmasks representing the 8 ranks on a single 8x8 bitboard. </summary>
        public static IReadOnlyList<ulong> BitRanks { get; } = new ulong[]
        {
            0xFF,
            0xFF00,
            0xFF0000,
            0xFF000000,
            0xFF00000000,
            0xFF0000000000,
            0xFF000000000000,
            0xFF00000000000000
        };

        #endregion

        #region Private Fields

        /* The following arrays, unlike the previous two, each contain 64 bitmasks
           corresponding to 64 possible positions on a single 8x8 bitboard. Hence
           there are duplicating masks but it's acceptable considering the performance
           boost we get by accessing any bitmask using just an already-known position.
           The same logic applies to the way attack masks are stored. */

        /// <summary> Bitmasks representing all top-left to bottom-right diagonals on a single 8x8 bitboard. </summary>
        private static readonly IReadOnlyList<ulong> _diagonalsA8H1 = new ulong[]
        {
            0x8040201008040201, 0x80402010080402, 0x804020100804, 0x8040201008, 0x80402010, 0x804020, 0x8040, 0x80,
            0x4020100804020100, 0x8040201008040201, 0x80402010080402, 0x804020100804, 0x8040201008, 0x80402010, 0x804020, 0x8040,
            0x2010080402010000, 0x4020100804020100, 0x8040201008040201, 0x80402010080402, 0x804020100804, 0x8040201008, 0x80402010, 0x804020,
            0x1008040201000000, 0x2010080402010000, 0x4020100804020100, 0x8040201008040201, 0x80402010080402, 0x804020100804, 0x8040201008, 0x80402010,
            0x804020100000000, 0x1008040201000000, 0x2010080402010000, 0x4020100804020100, 0x8040201008040201, 0x80402010080402, 0x804020100804, 0x8040201008,
            0x402010000000000, 0x804020100000000, 0x1008040201000000, 0x2010080402010000, 0x4020100804020100, 0x8040201008040201, 0x80402010080402, 0x804020100804,
            0x201000000000000, 0x402010000000000, 0x804020100000000, 0x1008040201000000, 0x2010080402010000, 0x4020100804020100, 0x8040201008040201, 0x80402010080402,
            0x100000000000000, 0x201000000000000, 0x402010000000000, 0x804020100000000, 0x1008040201000000, 0x2010080402010000, 0x4020100804020100, 0x8040201008040201
        };

        /// <summary> Bitmasks representing all bottom-left to top-right diagonals on a single 8x8 bitboard. </summary>
        private static readonly IReadOnlyList<ulong> _diagonalsA1H8 = new ulong[]
        {
            0x1, 0x102, 0x10204, 0x1020408, 0x102040810, 0x10204081020, 0x1020408102040, 0x102040810204080,
            0x102, 0x10204, 0x1020408, 0x102040810, 0x10204081020, 0x1020408102040, 0x102040810204080, 0x204081020408000,
            0x10204, 0x1020408, 0x102040810, 0x10204081020, 0x1020408102040, 0x102040810204080, 0x204081020408000, 0x408102040800000,
            0x1020408, 0x102040810, 0x10204081020, 0x1020408102040, 0x102040810204080, 0x204081020408000, 0x408102040800000, 0x810204080000000,
            0x102040810, 0x10204081020, 0x1020408102040, 0x102040810204080, 0x204081020408000, 0x408102040800000, 0x810204080000000, 0x1020408000000000,
            0x10204081020, 0x1020408102040, 0x102040810204080, 0x204081020408000, 0x408102040800000, 0x810204080000000, 0x1020408000000000, 0x2040800000000000,
            0x1020408102040, 0x102040810204080, 0x204081020408000, 0x408102040800000, 0x810204080000000, 0x1020408000000000, 0x2040800000000000, 0x4080000000000000,
            0x102040810204080, 0x204081020408000, 0x408102040800000, 0x810204080000000, 0x1020408000000000, 0x2040800000000000, 0x4080000000000000, 0x8000000000000000
        };

        /* The next 4 arrays contain special masks that are called projections. What are they, you may ask?
           Imagine a diagonal on a large composite board. The diagonal goes through multiple bitboards. If you look at
           the bitboards that form a diagonal themselves, they represent the same bitmask. However, the bit-diagonal
           may go through so-called side-boards - the ones that are not part of the mentioned bitboard-diagonal.

           For example, a queen on g5 on a 16x16 board covers the c1-p14 diagonal. The board is represented by 4 bitboards,
           and the diagonal goes through 3 of them: 12, 21, 22 (in matrix terms). Let's imagine each bitboard is a conventional
           chessboard consisting of a-h files and 1-8 ranks. With all that in mind, the bitboards 12 and 21 both hold the c1-h6
           diagonal. The bitboard 22 is different however: it holds the small a7-b8 diagonal. The last one is a projection of
           c1-h6.
           
           Now, diagonals can be projected upwards and downwards. In the previous example there's no upward projection because
           the diagonal doesn't pass the 11 bitboard - which is an upper bitboard in relation to the 21 where the queen is
           located. The 22 is considered a downward projection because it's the same as the lower bitboard 31 - if there was one. */

        /// <summary> Bitmasks representing upper projections of top-left to bottom-right diagonals. </summary>
        private static readonly IReadOnlyList<ulong> _projectionsUpA8H1 = new ulong[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0x80, 0, 0, 0, 0, 0, 0, 0,
            0x8040, 0x80, 0, 0, 0, 0, 0, 0,
            0x804020, 0x8040, 0x80, 0, 0, 0, 0, 0,
            0x80402010, 0x804020, 0x8040, 0x80, 0, 0, 0, 0,
            0x8040201008, 0x80402010, 0x804020, 0x8040, 0x80, 0, 0, 0,
            0x804020100804, 0x8040201008, 0x80402010, 0x804020, 0x8040, 0x80, 0, 0,
            0x80402010080402, 0x804020100804, 0x8040201008, 0x80402010, 0x804020, 0x8040, 0x80, 0
        };

        /// <summary> Bitmasks representing upper projections of bottom-left to top-right diagonals. </summary>
        private static readonly IReadOnlyList<ulong> _projectionsUpA1H8 = new ulong[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 1,
            0, 0, 0, 0, 0, 0, 1, 0x102,
            0, 0, 0, 0, 0, 1, 0x102, 0x10204,
            0, 0, 0, 0, 1, 0x102, 0x10204, 0x1020408,
            0, 0, 0, 1, 0x102, 0x10204, 0x1020408, 0x102040810,
            0, 0, 1, 0x102, 0x10204, 0x1020408, 0x102040810, 0x10204081020,
            0, 1, 0x102, 0x10204, 0x1020408, 0x102040810, 0x10204081020, 0x1020408102040
        };

        /// <summary> Bitmasks representing lower projections of top-left to bottom-right diagonals. </summary>
        private static readonly IReadOnlyList<ulong> _projectionsDownA8H1 = new ulong[]
        {
            0, 0x100000000000000, 0x201000000000000, 0x402010000000000, 0x804020100000000, 0x1008040201000000, 0x2010080402010000, 0x4020100804020100,
            0, 0, 0x100000000000000, 0x201000000000000, 0x402010000000000, 0x804020100000000, 0x1008040201000000, 0x2010080402010000,
            0, 0, 0, 0x100000000000000, 0x201000000000000, 0x402010000000000, 0x804020100000000, 0x1008040201000000,
            0, 0, 0, 0, 0x100000000000000, 0x201000000000000, 0x402010000000000, 0x804020100000000,
            0, 0, 0, 0, 0, 0x100000000000000, 0x201000000000000, 0x402010000000000,
            0, 0, 0, 0, 0, 0, 0x100000000000000, 0x201000000000000,
            0, 0, 0, 0, 0, 0, 0, 0x100000000000000,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        /// <summary> Bitmasks representing lower projections of bottom-left to top-right diagonals. </summary>
        private static readonly IReadOnlyList<ulong> _projectionsDownA1H8 = new ulong[]
        {
            0x204081020408000, 0x408102040800000, 0x810204080000000, 0x1020408000000000, 0x2040800000000000, 0x4080000000000000, 0x8000000000000000, 0,
            0x408102040800000, 0x810204080000000, 0x1020408000000000, 0x2040800000000000, 0x4080000000000000, 0x8000000000000000, 0, 0,
            0x810204080000000, 0x1020408000000000, 0x2040800000000000, 0x4080000000000000, 0x8000000000000000, 0, 0, 0,
            0x1020408000000000, 0x2040800000000000, 0x4080000000000000, 0x8000000000000000, 0, 0, 0, 0,
            0x2040800000000000, 0x4080000000000000, 0x8000000000000000, 0, 0, 0, 0, 0,
            0x4080000000000000, 0x8000000000000000, 0, 0, 0, 0, 0, 0,
            0x8000000000000000, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        /// <summary> Indicates whether BitBase has been initialized. </summary>
        private static bool _initialized;

        #endregion

        #region Public Methods

        /// <summary> Initializes BitBase. </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            // The relation between bit-positions and their respective Y-coordinates
            // does not ever change, so the conversion tables are calculated only once:
            GetBitConversionTables();

            _initialized = true;
        }

        /// <summary> Updates the matrix parameters and attack masks in accordance with N. </summary>
        public static void Update()
        {
            Events.OnOperationStarted("Pre-calculating...");
            Master.SW.Start();

            var rem = GetRemainder();
            Board.Reset(rem);
            ResetTrailingBitmasks(rem);
            ResetRightmostBitboardIndices();
            CalculateAttacks();

            Elapsed = Master.SW.Elapsed;
            Master.SW.Reset();
            Events.OnOperationFinished(Elapsed);
        }

        #endregion

        #region Private Update-Methods

        /// <summary> Initializes the BitToX and BitToY conversion tables. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetBitConversionTables()
        {
            BitToX = new Dictionary<ulong, int>();
            BitToY = new Dictionary<ulong, int>();

            // Loop through each bit-position:
            for (var i = 0; i < 64; i++)
            {
                var bitPos = 1UL << i;
                // The remainder represents the X-coordinate:
                BitToX.Add(bitPos, i % 8);
                // The quotient represents the Y-coordinate:
                BitToY.Add(bitPos, i / 8);
            }
        }

        /// <summary>
        /// Calculates the remainder, i.e. the number of trailing files/ranks, and returns the result.
        /// </summary>
        /// <returns> The remainder. </returns>
        private static int GetRemainder()
        {
            /* A remainder represents the number of files and ranks that are included in the topmost and leftmost bitboards.
               For example, in case of a 9x9 board there are 4 bitboards, and the remainder is 1. So the leftmost bitboard
               holds the first file only, the topmost bitboard holds just the first rank, and the top-left bitboard represents
               only 1 square (i9) - because it's the point where the only trailing file and rank intersect. */

            var rem = Master.N % 8;
            return rem > 0 ? rem : 8;

            /* A zero-remainder must be set to 8 because otherwise the utmost bitboards (which must be non-zero)
               will not contain any files or ranks at all. Quite the contrary, they must contain all files and
               ranks if N is a power of 8. */
        }

        /// <summary> Resets TrailingBitFiles and TrailingBitRanks for a given N. </summary>
        /// <param name="rem"> The remainder, i.e. the number of trailing files/ranks. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ResetTrailingBitmasks(int rem)
        {
            TrailingBitFiles = TrailingBitRanks = 0;

            for (var i = 0; i < rem; i++)
            {
                TrailingBitFiles |= BitFiles[i];
                TrailingBitRanks |= BitRanks[i];
            }
        }

        /// <summary> Re-populates RightmostBitboardIndices. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ResetRightmostBitboardIndices()
        {
            RightmostBitboardIndices = new int[Master.N];

            for (var i = 0; i < Master.N; i++)
                RightmostBitboardIndices[i] = i / 8 * Board.Dimension;
        }

        #endregion

        #region Precalculation of Attack Masks

        /// <summary> Calculates all attack masks and saves them to the Attacks variable. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateAttacks()
        {
            // The number of dictionaries corresponds to the number of bitboards in the matrix:
            Attacks = new Dictionary<ulong, ulong[]>[Board.BitboardCount];

            // Major offset is the distance between 2 contiguous bitboards forming a top-left to bottom-right diagonal.
            var majorOffset = Board.Dimension + 1;

            // Minor offset works the same way for a bottom-left to top-right diagonal. Notice the check before assignment:
            // the value must not equal 0. Otherwise, an infinite loop would follow later (offsets act as incremental values).
            var minorOffset = Board.Dimension > 1 ? Board.Dimension - 1 : 1;

            Parallel.For(0, Board.BitboardCount, Master.ThreadingOptions, i =>
            {
                // Instantiate a dictionary within the array:
                Attacks[i] = new Dictionary<ulong, ulong[]>();

                // Get the Y-coordinate, X-coordinate and inverted X-coordinate of the bitboard
                // (an inverted X-coordinate is basically a coordinate in the left-to-right system):
                var y = i / Board.Dimension;
                var x = i % Board.Dimension;
                var xInv = Board.Dimension - x - 1;

                // Iterate through each square, and get an attack mask.
                for (var j = 0; j < 64; j++)
                {
                    // An attack mask is represented by a full matrix of bitmasks that contain
                    // all attack-rays of a single queen placed on a given square.

                    var mtx = new ulong[Board.BitboardCount];

                    // The following methods will populate the bitboards with attack-rays:
                    DrawVertical(mtx, x, j);
                    DrawHorizontal(mtx, y, j);
                    DrawDiagonal(_diagonalsA8H1, mtx, i, j, x, y, majorOffset);
                    DrawDiagonal(_diagonalsA1H8, mtx, i, j, xInv, y, minorOffset);
                    DrawUpperProjections(mtx, i, j, x, xInv, y, majorOffset, minorOffset);
                    DrawLowerProjections(mtx, i, j, x, xInv, y, majorOffset, minorOffset);

                    // Finally, add the attack mask to the dictionary, and move on to the next position.
                    Attacks[i].Add(1UL << j, mtx);
                }
            });
        }

        /// <summary> Draws a vertical attack ray through an entire matrix. </summary>
        /// <param name="mtx"> A matrix (attack mask) to be modified. </param>
        /// <param name="x"> The X-coordinate of a bitboard containing the attacking queen. </param>
        /// <param name="pos"> The queen's local position on a bitboard. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DrawVertical(ulong[] mtx, int x, int pos)
        {
            var file = BitFiles[pos % 8];

            for (var i = x; i < Board.BitboardCount; i += Board.Dimension)
                mtx[i] |= file;
        }

        /// <summary> Draws a horizontal attack ray through an entire matrix. </summary>
        /// <param name="mtx"> A matrix (attack mask) to be modified. </param>
        /// <param name="y"> The Y-coordinate of a bitboard containing the attacking queen. </param>
        /// <param name="pos"> The queen's local position on a bitboard. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DrawHorizontal(ulong[] mtx, int y, int pos)
        {
            var fromIdx = y * Board.Dimension;
            var toIdx = fromIdx + Board.Dimension;
            var bitRank = BitRanks[pos / 8];

            for (var i = fromIdx; i < toIdx; i++)
                mtx[i] |= bitRank;
        }

        /// <summary> Draws a diagonal attack ray through an entire matrix. </summary>
        /// <param name="src"> A source array containing bitmasks of diagonals. </param>
        /// <param name="mtx"> A matrix (attack mask) to be modified. </param>
        /// <param name="bbIdx"> The index of a bitboard containing the attacking queen. </param>
        /// <param name="pos"> The queen's local position on a bitboard. </param>
        /// <param name="x"> The bitboard's X-coordinate. </param>
        /// <param name="y"> The bitboard's Y-coordinate. </param>
        /// <param name="ofs"> An offset (step) between 2 contiguous bitboards of the bitboard-diagonal. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DrawDiagonal(IReadOnlyList<ulong> src, ulong[] mtx, int bbIdx, int pos, int x, int y, int ofs)
        {
            /* The 2 distances determine the loop range, or how many bitboards
               the loop will go through before it reaches the edge. X and Y basically
               define which side of the board we will hit first while drawing the diagonal.
               To get the lower distance, we simply compare X and Y directly. To get the
               upper distance, however, we need to use the reversed values of Y and X -
               simply because of the opposite direction (like we just flipped the board). */

            var distDown = Math.Min(x, y);
            var distUp = Math.Min(Board.Dimension - x - 1, Board.Dimension - y - 1);

            /* The loop goes bottom-top, starting from the lowest bitboard -
               which is not necessarily the one containing the attacking queen. */

            var fromIdx = bbIdx - distDown * ofs;
            var toIdx = bbIdx + distUp * ofs;

            for (var i = fromIdx; i <= toIdx; i += ofs)
                mtx[i] |= src[pos];

            /* As you can see, the method is flexible, meaning that it uses
               whatever collection of bitmasks defined as source. */
        }

        /// <summary> Draws both upper projecting diagonals through an entire matrix. </summary>
        /// <param name="mtx"> A matrix (attack mask) to be modified. </param>
        /// <param name="bbIdx"> The index of a bitboard containing the attacking queen. </param>
        /// <param name="pos"> The queen's local position on a bitboard. </param>
        /// <param name="x"> The bitboard's X-coordinate. </param>
        /// <param name="xInv"> The bitboard's inverted X-coordinate. </param>
        /// <param name="y"> The bitboard's Y-coordinate. </param>
        /// <param name="ofsMjr"> A major offset (step) between 2 contiguous bitboards. </param>
        /// <param name="ofsMnr"> A minor offset (step) between 2 contiguous bitboards. </param>
        private static void DrawUpperProjections(ulong[] mtx, int bbIdx, int pos, int x, int xInv, int y, int ofsMjr, int ofsMnr)
        {
            if (y != Board.Dimension - 1)
            {
                /* The above condition means that the bitboard where the queen is located
                   is not in the last row in the matrix. Which means that we can simply use
                   the bitboard above it as a reference which will help us draw the projections. */

                var upperBbIdx = bbIdx + Board.Dimension;
                var upperBbY = y + 1;

                /* The projections are basically diagonals, so we call the appropriate method to
                   draw both of them. Notice that we use the upper bitboard's properties instead
                   of the one's where the queen is actually residing. */

                DrawDiagonal(_projectionsUpA8H1, mtx, upperBbIdx, pos, x, upperBbY, ofsMjr);
                DrawDiagonal(_projectionsUpA1H8, mtx, upperBbIdx, pos, xInv, upperBbY, ofsMnr);
            }
            else
            {
                /* Now we cannot just use an upper bitboard - because there is none. Instead
                   we simply use the next bitboard to the left (or to the right, depending
                   on the direction) - that will do just as well. But before that we make sure
                   we don't go off the edge. */

                if (bbIdx != Board.LastRightmostBitboardIndex)
                    DrawDiagonal(_projectionsUpA8H1, mtx, bbIdx - 1, pos, x - 1, y, ofsMjr);
                if (bbIdx != Board.LastBitboardIndex)
                    DrawDiagonal(_projectionsUpA1H8, mtx, bbIdx + 1, pos, xInv - 1, y, ofsMnr);
            }
        }

        /// <summary> Draws both lower projecting diagonals through an entire matrix. </summary>
        /// <param name="mtx"> A matrix (attack mask) to be modified. </param>
        /// <param name="bbIdx"> The index of a bitboard containing the attacking queen. </param>
        /// <param name="pos"> The queen's local position on a bitboard. </param>
        /// <param name="x"> The bitboard's X-coordinate. </param>
        /// <param name="xInv"> The bitboard's inverted X-coordinate. </param>
        /// <param name="y"> The bitboard's Y-coordinate. </param>
        /// <param name="ofsMjr"> A major offset (step) between 2 contiguous bitboards. </param>
        /// <param name="ofsMnr"> A minor offset (step) between 2 contiguous bitboards. </param>
        private static void DrawLowerProjections(ulong[] mtx, int bbIdx, int pos, int x, int xInv, int y, int ofsMjr, int ofsMnr)
        {
            /* This method is almost a twin brother of the previous one. The main difference
               is that here the lower bitboard is used as a reference, and the 1st rank is now
               the exception. Everything is pretty much straightforward. Maybe I should have
               merged these 2 methods into one, but I decided to leave them as they are for
               distinction and readability purposes. */

            if (y != 0)
            {
                var lowerBbIdx = bbIdx - Board.Dimension;
                var lowerBbY = y - 1;

                DrawDiagonal(_projectionsDownA8H1, mtx, lowerBbIdx, pos, x, lowerBbY, ofsMjr);
                DrawDiagonal(_projectionsDownA1H8, mtx, lowerBbIdx, pos, xInv, lowerBbY, ofsMnr);
            }
            else
            {
                if (bbIdx != Board.Dimension - 1)
                    DrawDiagonal(_projectionsDownA8H1, mtx, bbIdx + 1, pos, x + 1, y, ofsMjr);
                if (bbIdx != 0)
                    DrawDiagonal(_projectionsDownA1H8, mtx, bbIdx - 1, pos, xInv + 1, y, ofsMnr);
            }
        }

        #endregion
    }
}