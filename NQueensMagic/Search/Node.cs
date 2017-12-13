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