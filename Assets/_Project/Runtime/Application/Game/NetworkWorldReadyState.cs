using Unity.Netcode;

namespace PlanetIO.Application
{
    public sealed partial class NetworkWorldReadyState : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> _isReady = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<double> _clockOffset = new(0d);
        public override void OnNetworkSpawn()
        {
            _clockOffset.OnValueChanged += ClockChanged;
            _handoffPaused.OnValueChanged += PauseChanged;
            if (IsServer && _migration != null) _migration.PausePeers = PrepareHandoffAsync;
            if (_handoffPaused.Value) _migration?.BeginRecovery();
            OrbitClock.SetMigrationOffset(_clockOffset.Value);
        }
        public override void OnNetworkDespawn()
        {
            _clockOffset.OnValueChanged -= ClockChanged;
            _handoffPaused.OnValueChanged -= PauseChanged;
            if (_migration != null && _migration.PausePeers == PrepareHandoffAsync) _migration.PausePeers = null;
            OrbitClock.SetMigrationOffset(0d);
        }
        private void ClockChanged(double oldValue, double value) => OrbitClock.SetMigrationOffset(value);
        public void RestoreClock(double clock)
        {
            _clockOffset.Value = clock - NetworkManager.ServerTime.Time;
            OrbitClock.SetMigrationOffset(_clockOffset.Value);
        }
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
