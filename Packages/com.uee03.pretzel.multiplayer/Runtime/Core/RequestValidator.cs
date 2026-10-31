using System;

namespace Pretzel.Multiplayer
{
    /// <summary>Checks requests against <see cref="NetworkSettings"/> before any backend is touched. Returns null when the request is valid.</summary>
    public static class RequestValidator
    {
        public const int MaxNameLength = 24;
        public const int MaxSessionNameLength = 64;
        public const int MaxCodeLength = 32;

        public static MultiplayerError ValidateHost(SessionMode mode, HostRequest request, NetworkSettings settings)
        {
            MultiplayerError common = ValidateCommon(request, settings);
            if (common != null)
            {
                return common;
            }

            if (mode == SessionMode.Online)
            {
                if (request.MaxPlayers < 1 || request.MaxPlayers > settings.MaxPlayers)
                {
                    return Invalid($"Max players must be between 1 and {settings.MaxPlayers}.");
                }

                if (string.IsNullOrWhiteSpace(request.SessionName) || request.SessionName.Trim().Length > MaxSessionNameLength)
                {
                    return Invalid($"Session name must be 1 to {MaxSessionNameLength} characters.");
                }
            }

            return null;
        }

        public static MultiplayerError ValidateJoin(JoinRequest request, NetworkSettings settings)
        {
            MultiplayerError common = ValidateCommon(request, settings);
            if (common != null)
            {
                return common;
            }

            switch (request.Kind)
            {
                case JoinKind.Code:
                    if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > MaxCodeLength)
                    {
                        return Invalid("Enter the join code.");
                    }

                    break;
                case JoinKind.SessionId:
                    if (string.IsNullOrWhiteSpace(request.SessionId))
                    {
                        return Invalid("Choose a session.");
                    }

                    break;
            }

            return null;
        }

        /// <summary>Trims the name, falls back to <paramref name="fallback"/> when empty and caps the length.</summary>
        public static string NormalizeName(string name, string fallback = "Player")
        {
            string trimmed = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
            return trimmed.Length > MaxNameLength ? trimmed.Substring(0, MaxNameLength) : trimmed;
        }

        private static MultiplayerError ValidateCommon(SessionRequest request, NetworkSettings settings)
        {
            if (request == null)
            {
                return Invalid("Missing request.");
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (request.LocalPlayerCount < 1 || request.LocalPlayerCount > settings.MaxLocalPlayersPerClient)
            {
                return Invalid($"Local players must be between 1 and {settings.MaxLocalPlayersPerClient}.");
            }

            return null;
        }

        private static MultiplayerError Invalid(string message)
        {
            return new MultiplayerError(MultiplayerErrorCode.InvalidRequest, message);
        }
    }
}
