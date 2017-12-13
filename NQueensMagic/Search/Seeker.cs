using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace NQueensMagic.Search
{
    /// <summary> The base class for all classes that handle search. </summary>
    public abstract class Seeker
    {
        /// <summary> The elapsed time of the last search operation. </summary>
        public static TimeSpan Elapsed { get; private set; }

        /// <summary> The maximum number of attack masks per search path. </summary>
        /// <remarks>
        /// <n>Basically, a set consists of many queens, and each of them has her own attack mask.
        /// Because the search is done from bottom to top, i.e. per rank - not per file, attack
        /// masks build up, and therefore, the term 'column' is used.</n>
        /// <n>The number of attack masks does not equal the number of queens or N, however. This
        /// is because the first attack mask combines the masks of 2 queens at once, and the
        /// mask of the last queen doesn't count.</n>
        /// </remarks>
        protected int ColumnHeight;

        /// <summary> Universally runs the search algorithm. </summary>
        public void Run()
        {
            Events.OnOperationStarted("Searching...");
            Master.SW.Start();
            Solve();
            Elapsed = Master.SW.Elapsed;
            Master.SW.Reset();
            Events.OnFinishedSearching(Elapsed);
        }

        /// <summary> Any search function. </summary>
        protected abstract void Solve();

        /// <summary> Creates a search node, and adds it to the specified collection. </summary>
        /// <param name="nodes"> A node container. </param>
        /// <param name="rootFill"> The fill mask that excludes all unavailable positions. </param>
        /// <param name="bbIdx"> The index of the bitboard where the 2nd queen is supposed to be placed. </param>
        /// <param name="rootBbIdx"> The index of the first queen's bitboard. </param>
        /// <param name="rootBitPos"> The first queen's bit-position. </param>
        /// <param name="bitRank"> The bit-rank where the the 2nd queen is supposed to be placed. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AddNode(ConcurrentBag<Node> nodes, ulong[] rootFill, int bbIdx, int rootBbIdx, ulong rootBitPos, ulong bitRank)
        {
            /* NOTE: The root fill mask doesn't necessarily have to be just the attack mask of the first queen.
                     It can also include redundant bits on utmost bitboards in case N is not a power of 8. */

            // Exclude all unavailable positions from the rank.
            bitRank &= ~rootFill[bbIdx];

            // Are there any available positions left?
            while (bitRank > 0)
            {
                // Isolate 1 position as the least significant bit:
                var bitPos = bitRank & (0UL - bitRank);
                // Get an attack mask for the position:
                var attacks = BitBase.Attacks[bbIdx][bitPos];
                // Create a unique fill column specifically for this node:
                var fillColumn = new ulong[ColumnHeight][];
                // Initialize the first fill mask:
                var fill0 = new ulong[Board.BitboardCount];
                // Create a unique occupancy matrix for the node:
                var occupancy = new ulong[Board.BitboardCount];

                // Add the first 2 queens to the occupancy matrix.
                occupancy[rootBbIdx] = rootBitPos;
                occupancy[bbIdx] |= bitPos;

                // Make the first fill matrix a combination of the
                // root fill mask and the attack mask of the 2nd queen.
                for (var i = 0; i < Board.BitboardCount; i++)
                    fill0[i] = rootFill[i] | attacks[i];

                fillColumn[0] = fill0;
                nodes.Add(new Node(fillColumn, occupancy));
                // Reset the bitmask, i.e. move on to the next bit (if there is one):
                bitRank ^= bitPos;
            }
        }
    }
}