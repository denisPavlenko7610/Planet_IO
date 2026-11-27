using Unity.Netcode;

namespace PlanetIO.Application
{
    public sealed class NetworkWorldReadyState : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> _isReady = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public bool IsReady => _isReady.Value;

        public void MarkReady()
        {
            if (IsServer)
            {
                _isReady.Value = true;
            }
        }
    }
}
