namespace Pretzel.Samples.Events
{
    /// <summary>A message. Small readonly structs avoid allocations when published.</summary>
    // Value type: published often; a struct message avoids an allocation per publish.
    public readonly struct EnemyDefeated
    {
        public readonly string EnemyName;
        public readonly int Points;

        public EnemyDefeated(string enemyName, int points)
        {
            EnemyName = enemyName;
            Points = points;
        }
    }
}
