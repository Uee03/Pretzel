namespace SpaceInvaders.Coop
{
    /// <summary>Where a co-op ship's steering comes from. Tests replace it; the game uses <see cref="CoopShipInput"/>.</summary>
    public interface ICoopShipInput
    {
        /// <param name="localPlayerIndex">0 for the first player on this machine, 1 for the second.</param>
        /// <param name="acceptEveryScheme">True when this machine has one player, so both keyboard schemes steer it.</param>
        /// <returns>-1 (left) to 1 (right).</returns>
        float ReadHorizontal(int localPlayerIndex, bool acceptEveryScheme);

        bool ReadFire(int localPlayerIndex, bool acceptEveryScheme);
    }
}
