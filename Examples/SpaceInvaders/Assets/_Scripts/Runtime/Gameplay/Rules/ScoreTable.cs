using System;

namespace SpaceInvaders.Gameplay
{
    public static class ScoreTable
    {
        // The classic mystery ship pays by how many shots you have fired.
        private static readonly int[] _ufoPoints = { 100, 50, 50, 100, 150, 100, 100, 50, 300, 100, 100, 100, 50, 150, 100, 50 };

        public static int PointsFor(InvaderKind kind)
        {
            switch (kind)
            {
                case InvaderKind.Squid:
                    return 30;
                case InvaderKind.Crab:
                    return 20;
                case InvaderKind.Octopus:
                    return 10;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static int UfoPoints(int shotsFired)
        {
            if (shotsFired < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(shotsFired), "Shots cannot be negative.");
            }

            return _ufoPoints[shotsFired % _ufoPoints.Length];
        }
    }
}
