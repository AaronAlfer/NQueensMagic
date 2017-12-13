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