using System;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace PlanetIO.Infrastructure.Networking
{
    public sealed class SessionMigrationDataHandler : IMigrationDataHandler
    {
        private readonly MatchMigrationStore _store;
        public SessionMigrationDataHandler(MatchMigrationStore store) => _store = store;

        public byte[] Generate()
        {
            try
            {
                byte[] bytes = _store.Capture();
                return bytes.Length > 0 ? bytes : new byte[] { 0 };
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                return _store.LatestSnapshot ?? new byte[] { 0 };
            }
        }

        public void Apply(byte[] data)
        {
            try
            {
                _store.Accept(data);
            }
            catch (Exception error)
            {
                _store.Fail(error.Message);
                throw;
            }
        }
    }
}
