using System.Text;
using NUnit.Framework;
using PlanetIO.Infrastructure.Networking;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed class ConnectionApprovalTests
    {
        private const ulong GuestClientId = 7;

        private static readonly RoomConnectionSettings Room = new("ROOM", 4);

        private GameObject _networkObject;
        private ConnectionApprovalHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _networkObject = new GameObject("ConnectionApprovalTests");
            _handler = new ConnectionApprovalHandler(_networkObject.AddComponent<NetworkManager>());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_networkObject);
        }

        [Test]
        public void HostRequest_IsApprovedWithoutPayload()
        {
            NetworkManager.ConnectionApprovalResponse response = Approve(NetworkManager.ServerClientId, null);

            Assert.That(response.Approved, Is.True);
            Assert.That(response.CreatePlayerObject, Is.False);
        }

        [Test]
        public void ClientWithMatchingProtocol_IsApprovedWithoutAutomaticPlayerObject()
        {
            NetworkManager.ConnectionApprovalResponse response = Approve(GuestClientId, Payload(RoomRules.ProtocolVersion));

            Assert.That(response.Approved, Is.True);
            Assert.That(response.CreatePlayerObject, Is.False);
            Assert.That(response.Pending, Is.False);
            Assert.That(response.Reason, Is.Empty);
        }

        [Test]
        public void ClientWithoutPayload_IsRejectedAsInvalid()
        {
            NetworkManager.ConnectionApprovalResponse response = Approve(GuestClientId, null);

            Assert.That(response.Approved, Is.False);
            Assert.That(response.Reason, Is.EqualTo(SessionFailureReasons.InvalidPayload));
        }

        [TestCase("")]
        [TestCase("not json at all")]
        public void ClientWithMalformedPayload_IsRejectedAsInvalid(string text)
        {
            NetworkManager.ConnectionApprovalResponse response = Approve(GuestClientId, Encoding.UTF8.GetBytes(text));

            Assert.That(response.Approved, Is.False);
            Assert.That(response.Reason, Is.EqualTo(SessionFailureReasons.InvalidPayload));
        }

        [TestCase("planet-io/0")]
        [TestCase("")]
        [TestCase(null)]
        public void ClientWithOtherProtocol_IsRejectedAsVersionMismatch(string protocol)
        {
            NetworkManager.ConnectionApprovalResponse response = Approve(GuestClientId, Payload(protocol));

            Assert.That(response.Approved, Is.False);
            Assert.That(response.Reason, Is.EqualTo(SessionFailureReasons.VersionMismatch));
        }

        [Test]
        public void SinglePlayer_ApprovesHostAndRejectsEveryoneElse()
        {
            NetworkManager.ConnectionApprovalResponse host = new();
            ConnectionApprovalHandler.ApproveSinglePlayerConnection(
                new NetworkManager.ConnectionApprovalRequest { ClientNetworkId = NetworkManager.ServerClientId },
                host);

            NetworkManager.ConnectionApprovalResponse guest = new();
            ConnectionApprovalHandler.ApproveSinglePlayerConnection(
                new NetworkManager.ConnectionApprovalRequest { ClientNetworkId = GuestClientId },
                guest);

            Assert.That(host.Approved, Is.True);
            Assert.That(guest.Approved, Is.False);
            Assert.That(guest.Reason, Is.EqualTo(SessionFailureReasons.SinglePlayerOnly));
        }

        private NetworkManager.ConnectionApprovalResponse Approve(ulong clientId, byte[] payload)
        {
            NetworkManager.ConnectionApprovalResponse response = new();
            _handler.ApproveRoomConnection(
                new NetworkManager.ConnectionApprovalRequest { ClientNetworkId = clientId, Payload = payload },
                response,
                Room);
            return response;
        }

        private static byte[] Payload(string protocol)
        {
            return ConnectionApprovalHandler.SerializePayload(new ConnectionApprovalHandler.RoomConnectionPayload
            {
                Protocol = protocol,
                Nickname = "Guest"
            });
        }
    }
}
