using System;

namespace PlanetIO
{
    public interface ICloudSyncService
    {
        void SignIn(bool interactive, Action<bool> completed = null);

        void SyncNow();

        void RequestUpload();

        void FlushPending();
    }
}
