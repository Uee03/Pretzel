using System.Collections.Generic;

namespace Pretzel.Multiplayer.UI
{
    /// <summary>Everything the lobby screen shows, already formatted by the presenter.</summary>
    public sealed class LobbyViewModel
    {
        public string Title { get; }

        /// <summary>The join code line, empty in local mode.</summary>
        public string Code { get; }

        public string Info { get; }

        public IReadOnlyList<LobbyPlayerRow> Players { get; }

        public bool ShowReady { get; }

        public bool LocalIsReady { get; }

        public bool ShowStart { get; }

        public bool CanStart { get; }

        public LobbyViewModel(string title, string code, string info, IReadOnlyList<LobbyPlayerRow> players, bool showReady, bool localIsReady, bool showStart, bool canStart)
        {
            Title = title;
            Code = code;
            Info = info;
            Players = players;
            ShowReady = showReady;
            LocalIsReady = localIsReady;
            ShowStart = showStart;
            CanStart = canStart;
        }
    }
}
