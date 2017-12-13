using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NQueensMagic.Search
{
    /// <summary> Handles preset completion. </summary>
    public class CompletionSolver : Seeker
    {
        /// <summary> A completed queen set stored in matrix format. </summary>
        public ulong[] Solution { get; private set; }

        /// <summary> The index of the last available rank in the PresetMaker.FreeRanks. </summary>
        private int _lastFreeRankIdx = PresetMaker.FreeRanks.Count - 1;

        /// <summary> The details about the last free rank. </summary>
        private RankInfo _lastFreeRankInfo;

        /// <summary> The exclusive upper limit in the matrix. </summary>
        private int _bbLimit;

        /// <summary> Indicates whether the search has been cancelled. </summary>
        private bool _cancelled;

        /// <summary> Initializes a new CompletionSolver. </summary>
        public CompletionSolver()
        {
            _lastFreeRankInfo = GetFreeRankInfo(_lastFreeRankIdx);
            _bbLimit = _lastFreeRankInfo.BitboardIndex + Board.Dimension;
            ColumnHeight = _lastFreeRankIdx > 1 ? _lastFreeRankIdx - 1 : 1;
        }

        /// <summary> Attempts to find at least one solution to the current preset. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Solve()
        {
            /* The original fill mask contains not only redundant bits
               but also the attack masks of queens in the preset.

               Additionally, we get the information about the first 3 available
               ranks and store it in small data structures for later use. */

            var fillMask = PresetMaker.Preset.Fill;
            var ranksInfo = new RankInfo[3];

            for (var i = 0; i < 3; i++)
                ranksInfo[i] = GetFreeRankInfo(i);

            // Handle special cases when there are too little available ranks:
            switch (_lastFreeRankIdx)
            {
                case 0:
                    // If it's just 1 rank then create an almost-complete
                    // node out of the preset, and search the remaining rank.
                    var node = new Node(new ulong[][] { fillMask }, new ulong[Board.BitboardCount]);
                    SearchLast(node);
                    return;
                case 1:
                    // For completing 2 more ranks, there's a special function.
                    Complete2More(ranksInfo[0], ranksInfo[1], fillMask);
                    return;
            }

            /* Now gather root nodes. Notice the 'limit' variable: now that the 1st available
               rank may not actually be the 1st, the end of the row must be indicated specially. */

            var nodes = new ConcurrentBag<Node>();
            var limit = ranksInfo[1].BitboardIndex + Board.Dimension;

            Parallel.For(0, Master.N, Master.ThreadingOptions, i =>
            {
                var bbIdx = i / 8 + ranksInfo[0].BitboardIndex;
                var bitPos = ranksInfo[0].BitRank & BitBase.BitFiles[i % 8];

                // The first available rank is not guaranteed to be
                // completely free so we perform the following check:
                if ((bitPos & fillMask[bbIdx]) == 0)
                {
                    var rootFill = GetRootFillMatrix(bbIdx, bitPos, fillMask);

                    for (var j = ranksInfo[1].BitboardIndex; j < limit; j++)
                        AddNode(nodes, rootFill, j, bbIdx, bitPos, ranksInfo[1].BitRank);
                }
            });

            if (_lastFreeRankIdx == 2)
            {
                // This means that, after the nodes, there is only 1 rank left
                // to complete. So we call a special last-stage function:
                LastRankSearch(nodes);
            }
            else
            {
                // More than 1 more ranks, therefore pass the nodes to the common function:
                NormalSearch(nodes, ranksInfo[2]);
            }
        }

        /// <summary> Performs the main search stage using the recursive algorithm. </summary>
        /// <param name="nodes"> The root nodes. </param>
        /// <param name="r2"> Data regarding the 3rd available rank. </param>
        private void NormalSearch(ConcurrentBag<Node> nodes, RankInfo r2)
        {
            Parallel.ForEach(nodes, Master.ThreadingOptions, (node, loopState) =>
            {
                if (Search(node, r2.BitboardIndex, 2, r2.BitRank))
                {
                    loopState.Stop();
                    _cancelled = true;
                }
                else
                    Events.OnProgress();
            });
        }

        /// <summary> Performs search on the last remaining rank only. </summary>
        /// <param name="nodes"> The root nodes. </param>
        private void LastRankSearch(ConcurrentBag<Node> nodes)
        {
            Parallel.ForEach(nodes, Master.ThreadingOptions, (node, loopState) =>
            {
                if (SearchLast(node))
                    loopState.Stop();
                else
                    Events.OnProgress();
            });
        }

        /// <summary>
        /// Creates nodes on the 1st available rank, and then searches the last remaining one.
        /// </summary>
        /// <param name="r0"> Data regarding the 1st available rank. </param>
        /// <param name="r1"> Data regarding the 2nd available rank. </param>
        /// <param name="fillMask"> A root fill mask. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Complete2More(RankInfo r0, RankInfo r1, ulong[] fillMask)
        {
            Parallel.For(0, Master.N, Master.ThreadingOptions, (i, loopState) =>
            {
                var bbIdx = i / 8 + r0.BitboardIndex;
                var bitPos = r0.BitRank & BitBase.BitFiles[i % 8];

                if ((bitPos & fillMask[bbIdx]) == 0)
                {
                    var rootFill = GetRootFillMatrix(bbIdx, bitPos, fillMask);

                    var occupancy = new ulong[Board.BitboardCount];
                    occupancy[bbIdx] = bitPos;

                    if (SearchLast(new Node(new ulong[][] { rootFill }, occupancy)))
                        loopState.Stop();
                    else
                        Events.OnProgress();
                }
            });
        }

        /// <summary> Combines some original fill mask and an attack mask into one root fill matrix. </summary>
        /// <param name="bbIdx"> The index of the 1st bitboard in an active row. </param>
        /// <param name="bitPos"> The bit-position of an active queen. </param>
        /// <param name="fillMask"> Some original fill mask. </param>
        /// <returns> A root fill matrix. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong[] GetRootFillMatrix(int bbIdx, ulong bitPos, ulong[] fillMask)
        {
            var attacks = BitBase.Attacks[bbIdx][bitPos];
            var rootFill = new ulong[Board.BitboardCount];

            for (var j = 0; j < Board.BitboardCount; j++)
                rootFill[j] = attacks[j] | fillMask[j];

            return rootFill;
        }

        /// <summary>
        /// Safely generates a dataset regarding a free rank at the specified index, and returns the result.
        /// </summary>
        /// <param name="index"> A free rank's index. </param>
        /// <returns> A dataset regarding the rank. </returns>
        private RankInfo GetFreeRankInfo(int index)
        {
            return index <= _lastFreeRankIdx ? new RankInfo(PresetMaker.FreeRanks[index]) : new RankInfo();
        }

        /* The next 3 methods are very similar to those in TotalSolver. The main difference
           is that now we don't need to look in every corner, and all we want is 1 single solution,
           and as soon as it's found, we return true and stop the process.
           
           For comments regarding some implementation details of the following methods, take a look
           at the similar functions in TotalSolver. */

        /// <summary>
        /// A recursive function that looks for a way to place one more queen given some node.
        /// </summary>
        /// <param name="node"> An active search node. </param>
        /// <param name="bbIdx"> The index of the first bitboard in the active row. </param>
        /// <param name="rankIdx"> The index of the active available rank. </param>
        /// <param name="bitRank"> The bitboard representation of the active rank. </param>
        /// <returns> Was a solution found? </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Search(Node node, int bbIdx, int rankIdx, ulong bitRank)
        {
            if (_cancelled) return true;

            var columnIdx = rankIdx - 1;
            var nextRankIdx = rankIdx + 1;
            var nextRank = PresetMaker.FreeRanks[nextRankIdx];
            var nextBitRank = BitBase.BitRanks[nextRank % 8];
            var nextBbIdx = BitBase.RightmostBitboardIndices[nextRank];
            var upperBbIdx = bbIdx + Board.Dimension;

            for (var i = bbIdx; i < upperBbIdx; i++)
            {
                var mask = bitRank;

                for (var j = 0; j < columnIdx; j++)
                    mask &= ~node.FillColumn[j][i];

                while (mask > 0)
                {
                    var bitPos = mask & (0UL - mask);
                    node.Occupancy[i] |= bitPos;
                    node.FillColumn[columnIdx] = BitBase.Attacks[i][bitPos];

                    if (nextRankIdx == _lastFreeRankIdx)
                    {
                        if (SearchLast(node))
                            return true;
                    }
                    else
                    {
                        if (Search(node, nextBbIdx, nextRankIdx, nextBitRank))
                            return true;
                    }

                    node.Occupancy[i] ^= bitPos;
                    mask ^= bitPos;
                }
            }
            return false;
        }

        /// <summary> Looks for a way to place the last remaining queen. </summary>
        /// <param name="node"> An active search node. </param>
        /// <returns> Was a solution found? </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SearchLast(Node node)
        {
            for (var i = _lastFreeRankInfo.BitboardIndex; i < _bbLimit; i++)
            {
                var mask = _lastFreeRankInfo.BitRank;
                for (var j = 0; j < ColumnHeight; j++)
                    mask &= ~node.FillColumn[j][i];

                if (mask == 0) continue;

                AssignSolution(node.Occupancy, mask, i);
                return true;
            }
            return false;
        }

        /// <summary> Saves a solution to the global variable. </summary>
        /// <param name="occupancy"> An occupancy mask without the last queen. </param>
        /// <param name="finalBitPos"> The last queen's bit-position. </param>
        /// <param name="bbIdx"> The index of the bitboard where the last queen should be placed. </param>
        private void AssignSolution(ulong[] occupancy, ulong finalBitPos, int bbIdx)
        {
            var presetOccupancy = PresetMaker.Preset.Occupancy;
            var solution = new ulong[Board.BitboardCount];

            for (var i = 0; i < Board.BitboardCount; i++)
                solution[i] = presetOccupancy[i] | occupancy[i];

            solution[bbIdx] |= finalBitPos;
            Solution = solution;
        }
    }
}