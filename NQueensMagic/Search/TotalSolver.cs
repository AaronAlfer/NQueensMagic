using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NQueensMagic.Search
{
    /// <summary> Handles search for all solutions per N. </summary>
    public class TotalSolver : Seeker
    {
        /// <summary> All found solutions stored in matrix format. </summary>
        public ulong[][] Solutions { get; private set; } = new ulong[Stats.AllSolutionsUnique[Master.N - 1]][];

        /// <summary> Index of the last found solution. </summary>
        /// <remarks>
        /// Indicates what index the next solution should be added at. It gets incremented first, so it starts at -1.
        /// </remarks>
        private int _lastSolutionIndex = -1;

        /// <summary> Initializes a new TotalSolver. </summary>
        public TotalSolver()
        {
            /* 'Column height' or the number of individual attack masks is set to
               (N - 2) because the main search stage begins from rank 2, not rank 0.
               This means that the root nodes have at least 2 queens placed, so
               their masks are combined into 1. This is 1 mask off. Plus the last
               queen's mask does not count as it has no use: no more queens need to
               be placed. So this is another mask off: N - 2 in total. */

            ColumnHeight = Master.N - 2;
        }

        /// <summary> Finds all solutions and stores them in matrix format. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Solve()
        {
            /* We don't need to check every position in the first rank.
               In fact, we only need to check half of them (+1 if N is odd).
               This is because placing the first queen on the other half
               of the rank will produce the same fundamental results - only
               mirrored horizontally. */

            var halfWay = Master.N / 2 + Master.N % 2;

            /* The main search stage begins after all root nodes are collected.
               A root node is basically a description of an incomplete set
               containing the first 2 queens placed on the first 2 ranks. The
               reason for the whole concept is parallelism. Each thread will
               handle one path starting from some root node. Why 2 queens and
               not just the first one? Well, that way there are more starting
               points meaning more iterations, and therefore the parallel loop
               will work more efficiently. */

            var nodes = new ConcurrentBag<Node>();

            /* Also we need some original fill mask that takes redundant utmost
               bits into consideration. Those are bits that need to be excluded
               from the leftmost and topmost bitboards in case N is not a power of 8.
               By doing that right away, we don't need to worry about it anymore.
               The Empty() function handles the redundant bits. */

            var fillMask = QueenSet.Empty().Fill;

            // Gather the root nodes:
            Parallel.For(0, halfWay, Master.ThreadingOptions, i =>
            {
                // The index of the bitboard containing the examined square:
                var bbIdx = i / 8;
                // The first queen's bit-position:
                var bitPos = 1UL << (i % 8);
                // The first queen's attack mask:
                var attacks = BitBase.Attacks[bbIdx][bitPos];
                // The root fill mask will combine the attack mask and the original fill mask:
                var rootFill = new ulong[Board.BitboardCount];

                for (var j = 0; j < Board.BitboardCount; j++)
                    rootFill[j] = attacks[j] | fillMask[j];

                // Now go through the 2nd rank and get all the ways of placing
                // the 2nd queen, effectively collecting a set of root nodes:
                for (var j = 0; j < Board.Dimension; j++)
                    AddNode(nodes, rootFill, j, bbIdx, bitPos, 0xFF00);
            });

            // Perform the search per each root node:
            Parallel.ForEach(nodes, Master.ThreadingOptions, node =>
            {
                Search(node, 0, 2, 0xFF0000);

                // Notify the user that something is going on (helpful in case of big N):
                Events.OnProgress();
            });

            // All solutions should be found at this point.
            Events.OnAllSolutionsFound();
        }

        /// <summary>
        /// A recursive function that looks for a way to place one more queen given some node.
        /// </summary>
        /// <param name="node"> An active search node. </param>
        /// <param name="bbIdx"> The index of the first bitboard in the active row. </param>
        /// <param name="rank"> The active rank's index. </param>
        /// <param name="bitRank"> The bitboard representation of the active rank. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Search(Node node, int bbIdx, int rank, ulong bitRank)
        {
            // The active attack mask's index (the active attack mask is not yet added to the column):
            var columnIdx = rank - 1;

            // The location where the next iteration will take place:
            var nextRank = rank + 1;
            var nextBitRank = BitBase.BitRanks[nextRank % 8];
            var nextBbIdx = BitBase.RightmostBitboardIndices[nextRank];

            // The bitboard above the current one is the exclusive upper limit
            // of the following loop where all bitboards in the row are examined:
            var upperBbIdx = bbIdx + Board.Dimension;

            for (var i = bbIdx; i < upperBbIdx; i++)
            {
                // The bitmask is basically the rank minus attacked or invalid squares.

                var mask = bitRank;

                for (var j = 0; j < columnIdx; j++)
                    mask &= ~node.FillColumn[j][i];

                // Here is the basic bitmask search algorithm.
                while (mask > 0)
                {
                    var bitPos = mask & (0UL - mask);
                    node.Occupancy[i] |= bitPos;
                    node.FillColumn[columnIdx] = BitBase.Attacks[i][bitPos];

                    // Here goes the recursion. We handle the last rank differently, though.

                    if (nextRank == Board.LastRank)
                        SearchLast(node);
                    else
                        Search(node, nextBbIdx, nextRank, nextBitRank);

                    node.Occupancy[i] ^= bitPos;
                    mask ^= bitPos;
                }
            }
        }

        /// <summary> Looks for a way to place the last remaining queen. </summary>
        /// <param name="node"> An active search node. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SearchLast(Node node)
        {
            /* Go through the last row of bitboards, and find out whether
               there is at least one free square to place the last queen. */

            for (var i = Board.LastRightmostBitboardIndex; i < Board.BitboardCount; i++)
            {
                var mask = Board.LastBitRank;

                for (var j = 0; j < ColumnHeight; j++)
                    mask &= ~node.FillColumn[j][i];

                if (mask == 0) continue;

                AddSolution(node.Occupancy, mask, i);
                return;
            }
        }

        /// <summary> Adds a solution to the list. </summary>
        /// <param name="occupancy"> An occupancy mask without the last queen. </param>
        /// <param name="finalBitPos"> The last queen's bit-position. </param>
        /// <param name="bbIdx"> The index of the bitboard where the last queen should be placed. </param>
        private void AddSolution(ulong[] occupancy, ulong finalBitPos, int bbIdx)
        {
            var solution = new ulong[Board.BitboardCount];

            for (var j = 0; j < Board.BitboardCount; j++)
                solution[j] = occupancy[j];

            solution[bbIdx] |= finalBitPos;

            // A thread-safe way to increment the index and add a solution to the global variable:
            var idx = Interlocked.Increment(ref _lastSolutionIndex);
            Solutions[idx] = solution;
        }
    }
}