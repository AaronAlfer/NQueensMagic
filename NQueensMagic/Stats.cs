using System.Collections.Generic;

namespace NQueensMagic
{
    /// <summary> Contains some statistical data regarding the N Queens Puzzle. </summary>
    public static class Stats
    {
        /// <summary>
        /// The sequence of total numbers of solutions for every N up to the maximum value.
        /// </summary>
        public static IReadOnlyList<int> AllSolutionsTotal { get; } = new int[]
        {
            1, // 1
            0, // 2
            0, // 3
            2, // 4
            10, // 5
            4, // 6
            40, // 7
            92, // 8
            352, // 9
            724, // 10
            2680, // 11
            14200, // 12
            73712, // 13
            365596, // 14
            2279184, // 15
            14772512 // 16
        };

        /// <summary>
        /// The sequence of total numbers of 'unique' solutions for every N up to the maximum value.
        /// </summary>
        /// <remarks>
        /// A 'unique' solution is not a fundamental solution. You may notice that with every N that is even
        /// the number of 'unique' solutions is exactly half the corresponding total number of solutions.
        /// This is because the search algorithm considers only one half of the first rank. Placing queens on
        /// the other half would give the exact same results - but mirrored. So instead of doing that, it finds
        /// only the 'unique' solutions, and to get the rest one simply needs to flip the first half of them.
        /// As for the odd values of N, all solutions that have a queen placed in the middle of the 1st rank are
        /// obtained during the search and therefore added to the list of 'unique' solutions.
        /// </remarks>
        public static IReadOnlyList<int> AllSolutionsUnique { get; } = new int[]
        {
            0, // 1
            0, // 2
            0, // 3
            1, // 4
            6, // 5
            2, // 6
            23, // 7
            46, // 8
            203, // 9
            362, // 10
            1515, // 11
            7100, // 12
            40891, // 13
            182798, // 14
            1248961, // 15
            7386256 // 16
        };
    }
}