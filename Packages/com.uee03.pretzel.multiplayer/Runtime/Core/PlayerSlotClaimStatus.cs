namespace Pretzel.Multiplayer
{
    public enum PlayerSlotClaimStatus
    {
        /// <summary>Fresh slots were assigned.</summary>
        Claimed = 0,

        /// <summary>The player returned within the grace window and gets the same slots and objects back.</summary>
        Resumed = 1,

        /// <summary>Not enough free slots.</summary>
        Full = 2,

        /// <summary>The same player key is already connected. The old connection has to time out first.</summary>
        DuplicateKey = 3,
    }
}
