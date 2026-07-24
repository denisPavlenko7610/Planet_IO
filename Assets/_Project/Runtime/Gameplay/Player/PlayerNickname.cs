using System;
using Unity.Collections;
using Unity.Netcode;
using VContainer;

namespace PlanetIO
{
    public sealed class PlayerNickname : NetworkBehaviour
    {
        private readonly NetworkVariable<FixedString64Bytes> _networkNickname =
            new(
                new FixedString64Bytes(NicknameRules.DefaultNickname),
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private IPlayerProfileService _playerProfileService;

        public event Action<string> NicknameChanged;

        public string Nickname =>
            NicknameRules.Normalize(_networkNickname.Value.ToString());

        [Inject]
        public void Construct(IPlayerProfileService playerProfileService)
        {
            _playerProfileService = playerProfileService
                ?? throw new ArgumentNullException(nameof(playerProfileService));
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _networkNickname.OnValueChanged += OnNetworkNicknameChanged;

            if (IsOwner)
            {
                PublishLocalNickname();
            }

            NicknameChanged?.Invoke(Nickname);
        }

        public override void OnNetworkDespawn()
        {
            _networkNickname.OnValueChanged -= OnNetworkNicknameChanged;
            base.OnNetworkDespawn();
        }

        private void PublishLocalNickname()
        {
            string nickname = NicknameRules.Normalize(
                _playerProfileService?.Nickname);
            FixedString64Bytes serializedNickname = new(nickname);

            if (IsServer)
            {
                _networkNickname.Value = serializedNickname;
            }
            else
            {
                SubmitNicknameRpc(serializedNickname);
            }
        }

        [Rpc(SendTo.Server)]
        private void SubmitNicknameRpc(FixedString64Bytes nickname)
        {
            _networkNickname.Value = new FixedString64Bytes(
                NicknameRules.Normalize(nickname.ToString()));
        }

        private void OnNetworkNicknameChanged(
            FixedString64Bytes previousNickname,
            FixedString64Bytes currentNickname)
        {
            NicknameChanged?.Invoke(
                NicknameRules.Normalize(currentNickname.ToString()));
        }
    }
}
