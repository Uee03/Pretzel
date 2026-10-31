using System;
using System.Text;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>Encodes and validates connection approval payloads, enforcing the configured size limit before any parsing.</summary>
    public static class ConnectionPayloadCodec
    {
        private const int MaxKeyLength = 64;

        /// <exception cref="InvalidOperationException">The encoded payload is larger than <paramref name="maxBytes"/>.</exception>
        public static byte[] Encode(ConnectionPayload payload, int maxBytes)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            if (bytes.Length > maxBytes)
            {
                throw new InvalidOperationException($"The connection payload is {bytes.Length} bytes, the limit is {maxBytes}.");
            }

            return bytes;
        }

        /// <summary>Returns false with a reason that is safe to send to the client.</summary>
        public static bool TryDecode(byte[] data, int maxBytes, int maxLocalPlayers, out ConnectionPayload payload, out string error)
        {
            payload = null;
            if (data == null || data.Length == 0)
            {
                error = "Missing connection data.";
                return false;
            }

            if (data.Length > maxBytes)
            {
                error = "Connection data is too large.";
                return false;
            }

            ConnectionPayload decoded;
            try
            {
                decoded = JsonUtility.FromJson<ConnectionPayload>(Encoding.UTF8.GetString(data));
            }
            catch (ArgumentException)
            {
                error = "Connection data is not valid.";
                return false;
            }

            if (decoded == null || decoded.Version != ConnectionPayload.CurrentVersion)
            {
                error = "Connection data version is not supported.";
                return false;
            }

            if (string.IsNullOrEmpty(decoded.PlayerKey) || decoded.PlayerKey.Length > MaxKeyLength)
            {
                error = "Connection data has no valid player key.";
                return false;
            }

            if (decoded.LocalPlayerCount < 1 || decoded.LocalPlayerCount > maxLocalPlayers)
            {
                error = "Too many local players.";
                return false;
            }

            payload = decoded;
            error = null;
            return true;
        }
    }
}
