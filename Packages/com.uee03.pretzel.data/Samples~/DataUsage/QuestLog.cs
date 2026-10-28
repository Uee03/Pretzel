using System;
using System.Collections.Generic;
using Pretzel.Data;
using UnityEngine.Scripting;

namespace Pretzel.Samples.Data
{
    [Serializable, Preserve, SchemaVersion(1)]
    public sealed class QuestLog
    {
        public SerializableGuid questId = SerializableGuid.NewGuid();
        public string title;
        public List<IQuestReward> rewards = new();
    }
}
