using System;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace PlanetIO.Infrastructure.Networking
{
    public sealed class ConnectionApprovalHandler
    {
        private readonly NetworkManager _networkManager;

        public ConnectionApprovalHandler(NetworkManager networkManager)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
        }

        public void ApproveRoomConnection(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response,
            RoomConnectionSettings currentRoom)
        {
            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                Approve(response);
                return;
            }

            if (!TryDeserializePayload(request.Payload, out RoomConnectionPayload payload))
            {
                Reject(response, "Invalid connection payload.");
                return;
            }

            if (!string.Equals(payload.Protocol, RoomRules.ProtocolVersion, StringComparison.Ordinal))
            {
                Reject(response, "Client version does not match room version.");
                return;
            }

            if (_networkManager.ConnectedClientsIds.Count >= currentRoom.MaxPlayers)
            {
                Reject(response, "Room is full.");
                return;
            }

            Approve(response);
        }

        public static void ApproveSinglePlayerConnection(NetworkManager.ConnectionApprovalRequest request,
			NetworkManager.ConnectionApprovalResponse response)
        {
            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                Approve(response);
            }
            else
            {
                Reject(response, "This session is running in single player mode.");
            }
        }

        public static void Approve(NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = string.Empty;
        }

        public static void Reject(NetworkManager.ConnectionApprovalResponse response, string reason)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = reason;
        }

        public static byte[] SerializePayload(RoomConnectionPayload payload)
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        }

        public static bool TryDeserializePayload(byte[] bytes, out RoomConnectionPayload payload)
        {
            payload = null;
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            try
            {
                payload = JsonUtility.FromJson<RoomConnectionPayload>(Encoding.UTF8.GetString(bytes));
                return payload != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Serializable]
        public sealed class RoomConnectionPayload
        {
            public string Protocol;
            public string Nickname;
        }
    }
}
