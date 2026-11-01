using System.Threading;
using Pretzel.Scenes;
using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>
    /// The work the Game scene does behind the loading screen: it warms the invader pools a few per frame, so the first wave spawns
    /// without a hitch and the loading bar moves while it happens.
    /// </summary>
    public sealed class LevelSetup : MonoBehaviour
    {
        [SerializeField] private InvaderFormation _formation;
        [SerializeField] private WaveSet _waves;

        public async Awaitable PrepareAsync(SceneSetupContext context)
        {
            context.ReportStatus("Warming up the invaders");
            WaveDefinition first = _waves.GetWave(1);
            await _formation.PrewarmAsync(first.Columns, first.Rows, context.Progress, context.CancellationToken);
        }
    }
}
