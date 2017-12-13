using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NQueensMagic
{
    /// <summary> Manages presets, i.e. incomplete sets of queens. </summary>
    /// <remarks>
    /// Combines the following activities: random preset generation,
    /// handling manually defined presets, control over the preset attributes.
    /// </remarks>
    public static class PresetMaker
    {
        #region Properties

        /// <summary> The current preset of queens. </summary>
        public static QueenSet Preset { get; private set; }

        /// <summary> Number of queens in the preset. </summary>
        public static int Size
        {
            get => _size;
            set
            {
                if (value < 1 || value >= Master.N)
                {
                    Events.OnVariableOutOfRange("Preset size", 1, Master.N - 1);
                    return;
                }

                _size = value;
                Events.OnVariableChanged("Preset size", value);

                GenerateRandomPreset();

                /* By generating a random preset, we give the program something
               to work with right off the bat. This accomplishes 2 things:
               
               A) It ensures that at any point there is some preset ready, so no need
               to handle useless search calls.

               B) It makes life a bit easier as the user doesn't need to type a separate
               command for preset generation each time. */
            }
        }

        /// <summary> The default preset size. </summary>
        public static int DefaultSize => Master.N / 2;

        /// <summary> A list of all free ranks in the current preset. </summary>
        public static List<int> FreeRanks { get; } = new List<int>();

        /// <summary> The elapsed time of the last random-preset-generation procedure. </summary>
        public static TimeSpan Elapsed { get; private set; }

        #endregion

        #region Private Fields

        /// <summary> Indicates whether PresetMaker has been initialized. </summary>
        private static bool _initialized;

        private static int _size;

        #endregion

        #region Public Methods

        /// <summary> Initializes the Preset Maker. </summary>
        public static void Initialize()
        {
            // This prevents the initialization from happening more than once.
            if (_initialized) return;

            Size = DefaultSize;
            _initialized = true;
        }

        /// <summary> Clears the preset. It's called upon exiting the Completion mode. </summary>
        public static void Close()
        {
            Preset = new QueenSet();
            FreeRanks.Clear();
            _initialized = false;
        }

        /// <summary> Updates the preset size in proportion to N. </summary>
        /// <param name="oldN"> The previous value of N. </param>
        public static void UpdateProportionally(int oldN)
        {
            // If the old value is 0, which happens on the first run, assign to default.
            var newSize = oldN > 0 ? _size * Master.N / oldN : DefaultSize;
            Size = newSize;
        }

        /// <summary>
        /// Tests a user-specified preset for validity and makes it the current one, if proven valid.
        /// </summary>
        /// <param name="arr"> An array of Y-coordinates. </param>
        /// <returns> Does a given array represent a valid set of non-attacking queens? </returns>
        public static bool TryManualPreset(int[] arr)
        {
            if (!arr.ToQueenSet(out var preset))
                return false;

            // Update the size as the user may have entered any number of queens.
            _size = arr.NumberOfQueens();
            Events.OnVariableChanged("Preset size", _size);

            // Check the number of available ranks.
            if (!TestFreeRanks(preset.Fill, out var freeRanks))
                return false;

            Preset = preset;
            UpdateFreeRanks(freeRanks);

            // Reset the elapsed time as it's only relevant in case of random preset
            // generation - not user input. This way we avoid further confusion.
            Elapsed = TimeSpan.Zero;
            return true;
        }

        /// <summary>
        /// Repeatedly attempts to generate a random non-conflicting preset until it succeeds to do so.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GenerateRandomPreset()
        {
            Events.OnOperationStarted("Generating a random preset...");
            Master.SW.Start();

            var attempts = 0;
            var success = false;
            var padlock = new object();

            /* We want to involve as many threads as possible in the process.
               This way multiple seed values are tried out simultaneously
               which increases the chance of finding a valid combination sooner.
               This becomes especially important when the N/Size ratio is very high. */

            Parallel.For(0, Master.NumberOfThreads, Master.ThreadingOptions, i =>
            {
                // We use a unique identifier generator for our seed value because
                // it can produce different random numbers simultaneously for multiple threads.
                var random = new Random(Guid.NewGuid().GetHashCode());

                while (!success)
                {
                    /* First, we try to generate a random preset in which queens do not attack each other.
                       If the attempt was successful it still doesn't mean that the puzzle can be solved.
                       So another test is launched to count all free ranks on the board. In a proper preset
                       the number of ranks must equal the number of queens left. When both conditions are
                       met the preset is confirmed as valid and that's it.
                       
                       P.S. Although, even if both conditions are met there's no guarantee that the preset
                       can be completed later on: having free ranks is not enough. The only way to find out
                       is to actually try and find a solution. So this method may need to be called multiple
                       times in order to generate a solvable preset, especially with high N/Size ratios. */

                    if (TryRandomPreset(random, out var preset)
                    && TestFreeRanks(preset.Fill, out var freeRanks))
                    {
                        // Making sure only 1 thread can modify global values at a time:
                        lock (padlock)
                        {
                            Preset = preset;
                            UpdateFreeRanks(freeRanks);
                            success = true;
                        }
                    }

                    Interlocked.Increment(ref attempts);
                }
            });

            Elapsed = Master.SW.Elapsed;
            Master.SW.Reset();
            Serializer.SerializePreset();
            Events.OnOperationFinished(Elapsed, $"Attempts: {attempts}", $"Preset: {Serializer.Preset}");
        }

        #endregion

        #region Private Methods

        /// <summary> Tries to build a random preset of non-attacking queens. </summary>
        /// <param name="random"> A random number generator. </param>
        /// <param name="preset"> Obtained preset. </param>
        /// <returns> Was an attempt successful? </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryRandomPreset(Random random, out QueenSet preset)
        {
            preset = QueenSet.Empty();
            var added = 0;

            // Repeat the cycle until all queens are added to the preset.
            while (added < _size)
            {
                // Randomly pick a bitboard from the matrix.
                var bbIdx = random.Next(0, Board.BitboardCount);
                var mask = ~preset.Fill[bbIdx];

                // Evaluates to true if the bitboard has no 0-bits left.
                if (mask == 0)
                {
                    // If every other bitboard is full then the process can't continue.
                    if (IsBoardFilledUp(preset.Fill))
                        return false;
                    continue;
                }

                // Obtain the list of all free squares (i.e. bit-positions) on the bitboard.
                var bits = GetAvailableBits(mask);
                // Randomly pick a bit-position from the list.
                var pos = bits[random.Next(0, bits.Count)];
                var attacks = BitBase.Attacks[bbIdx][pos];

                // Populate the fill mask and the occupancy mask.

                for (var i = 0; i < Board.BitboardCount; i++)
                    preset.Fill[i] |= attacks[i];

                preset.Occupancy[bbIdx] |= pos;
                added++;
            }
            return true;
        }

        /// <summary> Gets the list of all available bit-positions (squares) on a bitboard. </summary>
        /// <param name="mask"> A fill mask. </param>
        /// <returns> A list of bit-positions. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<ulong> GetAvailableBits(ulong mask)
        {
            var bits = new List<ulong>();
            while (mask > 0)
            {
                // Obtain the least significant bit:
                var bit = mask & (0UL - mask);
                // Add the result to the list as a viable position:
                bits.Add(bit);
                // Reset the mask:
                mask ^= bit;
            }
            return bits;
        }

        /// <summary>
        /// Determines if every bitboard from a given set is filled, i.e. has no 0-bits left to populate.
        /// </summary>
        /// <param name="fillSet"> A set of bitboards representing a fill mask. </param>
        /// <returns> Is each bitboard filled? </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBoardFilledUp(ulong[] fillSet)
        {
            // Iterate through each bitboard in the matrix.
            for (var i = fillSet.Length - 1; i >= 0; i--)
            {
                // True if there are no 0-bits.
                if (fillSet[i] == ulong.MaxValue)
                    continue;

                return false;
            }
            return true;
        }

        /// <summary> Obtains the list of free ranks in a given set.
        /// Returns false if the number is not sufficient. </summary>
        /// <param name="fillSet"> A set of bitboards representing a fill mask. </param>
        /// <param name="freeRanks"> Obtained list of free ranks. </param>
        /// <returns> Is the number of free ranks sufficient? </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TestFreeRanks(ulong[] fillSet, out List<int> freeRanks)
        {
            freeRanks = new List<int>();
            var count = 0;

            // Iterate through each rank of the board.
            for (var i = 0; i < Master.N; i++)
            {
                // Get the bit-rank, the bitboard index and the leftmost bitboard index in the row.
                var bRank = BitBase.BitRanks[i % 8];
                var bbIdx = i / 8 * Board.Dimension;
                var bbMax = bbIdx + Board.Dimension;

                // Iterate through each bitboard in the row.
                for (var j = bbIdx; j < bbMax; j++)
                {
                    // True if the entire rank is occupied (under attack).
                    if ((fillSet[j] & bRank) == bRank)
                        continue;

                    // 1 vacant position is enough for the rank to be considered 'free'.
                    freeRanks.Add(i);
                    count++;
                    break;
                }
            }

            // Is the number of free ranks the same as the number of queens left?
            return count == Master.N - _size;
        }

        /// <summary> Updates the list of free ranks. </summary>
        /// <param name="freeRanks"> A new list of free ranks. </param>
        private static void UpdateFreeRanks(List<int> freeRanks)
        {
            FreeRanks.Clear();
            FreeRanks.AddRange(freeRanks);
        }

        #endregion
    }
}