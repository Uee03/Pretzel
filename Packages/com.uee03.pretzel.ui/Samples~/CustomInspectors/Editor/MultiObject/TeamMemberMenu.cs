using UnityEditor;
using UnityEngine;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    internal static class TeamMemberMenu
    {
        private const int Count = 3;

        // Creates several members and selects them all, so the multi-object inspector can be tried straight away.
        [MenuItem(SampleObjects.MenuRoot + "Create/Team Members (3)")]
        private static void Create()
        {
            GameObject[] created = new GameObject[Count];
            for (int i = 0; i < Count; i++)
            {
                TeamMember member = SampleObjects.CreateComponent<TeamMember>("Team Member " + (i + 1));
                member.memberName = "Member " + (i + 1);
                member.team = (TeamColor)(i % 3);
                member.transform.position = new Vector3(i * 2f, 0f, 0f);
                created[i] = member.gameObject;
            }

            Selection.objects = created;
        }
    }
}
