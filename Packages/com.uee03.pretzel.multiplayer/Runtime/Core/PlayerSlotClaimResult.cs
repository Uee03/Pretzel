namespace Pretzel.Multiplayer
{
    public sealed class PlayerSlotClaimResult
    {
        public PlayerSlotClaimStatus Status { get; }

        /// <summary>Null when the claim failed.</summary>
        public PlayerSlotReservation Reservation { get; }

        public bool Succeeded => Status == PlayerSlotClaimStatus.Claimed || Status == PlayerSlotClaimStatus.Resumed;

        public PlayerSlotClaimResult(PlayerSlotClaimStatus status, PlayerSlotReservation reservation)
        {
            Status = status;
            Reservation = reservation;
        }
    }
}
