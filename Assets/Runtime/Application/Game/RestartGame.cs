using UnityEngine;
using VContainer;

namespace Planet_IO.Application
{
    public sealed class RestartGame : MonoBehaviour
    {
        private INetworkSessionService _session;

        [Inject]
        public void Construct(INetworkSessionService session)
        {
            _session = session;
        }

        public async void Restart()
        {
            if (_session != null)
            {
                await _session.ShutdownAndReturnToMenuAsync();
            }
        }
    }
}
