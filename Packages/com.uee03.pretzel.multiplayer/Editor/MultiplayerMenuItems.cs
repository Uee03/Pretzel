using UnityEditor;
using UnityEngine;

namespace Pretzel.Multiplayer.Editor
{
    internal static class MultiplayerMenuItems
    {
        private const string Root = "GameObject/Pretzel/Multiplayer/";

        [MenuItem(Root + "Menu", false, 10)]
        private static void CreateMenu(MenuCommand command)
        {
            MultiplayerSceneBuilder.CreateMenu(command.context as GameObject);
        }

        [MenuItem(Root + "Network Manager (Pretzel defaults)", false, 11)]
        private static void CreateNetworkManager(MenuCommand command)
        {
            MultiplayerSceneBuilder.CreateNetworkManager(command.context as GameObject);
        }
    }
}
