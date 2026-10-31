using System.Collections.Generic;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Receives every IReward registration, in registration order. Adding a reward needs no change here.</summary>
    public sealed class RewardDemo : IStartable
    {
        private readonly ISampleLog _log;
        private readonly IReadOnlyList<IReward> _rewards;

        public RewardDemo(ISampleLog log, IReadOnlyList<IReward> rewards)
        {
            _log = log;
            _rewards = rewards;
        }

        public void Start()
        {
            List<string> descriptions = new(_rewards.Count);
            foreach (IReward reward in _rewards)
            {
                descriptions.Add(reward.Describe());
            }

            _log.Write("09 Collections", $"{_rewards.Count} rewards: {string.Join(", ", descriptions)}");
        }
    }
}
