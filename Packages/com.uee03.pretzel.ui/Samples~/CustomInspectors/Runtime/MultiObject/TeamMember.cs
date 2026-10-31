using UnityEngine;

namespace Pretzel.Samples.UI.CustomInspectors
{
    /// <summary>Target of the MultiObject inspector: select several of these to edit them together.</summary>
    [AddComponentMenu("Pretzel Samples/Team Member")]
    public sealed class TeamMember : MonoBehaviour
    {
        public string memberName = "Member";
        public TeamColor team;
        [Min(1)] public int level = 1;
    }
}
