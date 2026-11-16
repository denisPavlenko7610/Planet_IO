using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using VContainer;

namespace PlanetIO.Infrastructure.Networking
{
    public sealed partial class NetworkSessionService
    {
        private MatchMigrationStore _migration;
        private int _recoveryGeneration;
        private bool _finalUpload;
        private bool _recoveryWatchRunning;
        private int _uploadGeneration;
        private Task _snapshotUpload;

        [Inject] public void ConfigureMigration(MatchMigrationStore store)
        {
            _migration = store;
            store.RecoveryStarted += BeginNetworkRecovery;
            store.RecoveryCompleted += RecoveryFinished;
        }

        private void RecoveryFinished()
        {
            _recoveryWatchRunning = false;
            _recoveryGeneration++;
        }

        private async Awaitable UploadSnapshotsAsync(int generation)
        {
            while (!_shutdownRequested && generation == _uploadGeneration && _sessionConnector.ActiveSession != null)
            {
                if (Mode == NetworkSessionMode.Host &&
                    _networkManager.IsServer &&
                    _sessionConnector.ActiveSession.PlayerCount > 1)
                {
                    try
                    {
                        byte[] data = _migration.Capture();
                        if (data.Length > 0)
                        {
                            _snapshotUpload = _sessionConnector.ActiveSession.AsHost()
                                .SetHostMigrationDataAsync(data, TimeSpan.FromSeconds(5));
                            await _snapshotUpload;
                        }
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning("Migration snapshot upload failed: " + error.Message);
                    }
                }
                float next = Time.realtimeSinceStartup + 1f;
                while (!_shutdownRequested && generation == _uploadGeneration && Time.realtimeSinceStartup < next)
                {
                    await Awaitable.NextFrameAsync();
                }
            }
        }

        private void AttachMigrationSession(ISession session)
        {
            var clients = _migration.Clients.ToArray();
            _migration.StartMatch(session.Id);
            _migration.LocalIdentity = _sessionConnector.LocalIdentity;
            foreach (var client in clients)
            {
                _migration.Clients[client.Key] = client.Value;
            }
            _ = UploadSnapshotsAsync(++_uploadGeneration);
        }

        private bool CanRecoverHost() => !_shutdownRequested &&
            _sessionConnector.ActiveSession != null &&
            Mode is NetworkSessionMode.Host or NetworkSessionMode.Client &&
            State is NetworkSessionState.InGame or NetworkSessionState.Loading or NetworkSessionState.Migrating;

        private void BeginNetworkRecovery()
        {
            if (!CanRecoverHost() || _recoveryWatchRunning)
            {
                return;
            }

            _recoveryWatchRunning = true;
            _migration.BeginRecovery();
            SetState(NetworkSessionState.Migrating, "Reconnecting to the match");
            _ = WatchRecoveryAsync(++_recoveryGeneration);
        }

        private async Awaitable WatchRecoveryAsync(int generation)
        {
            float deadline = Time.realtimeSinceStartup + 45f;
            while (!_shutdownRequested && generation == _recoveryGeneration && _migration.IsRecovering)
            {
                if (_migration.Failure != null || Time.realtimeSinceStartup > deadline)
                {
                    _migration.Fail(_migration.Failure ?? "Host migration timed out.");
                    await RecoverFromDisconnectAsync(_migration.Failure);
                    return;
                }
                await Awaitable.NextFrameAsync();
            }
            if (generation == _recoveryGeneration)
            {
                _recoveryWatchRunning = false;
            }
        }

        private async Task RestartMigratedNetworkAsync(bool isHost)
        {
            if (_shutdownRequested)
            {
                throw new OperationCanceledException("Session was closed during migration.");
            }

            BeginNetworkRecovery();
            Mode = isHost ? NetworkSessionMode.Host : NetworkSessionMode.Client;
            SubscribeSceneManager();
            if (isHost)
            {
                if (!_migration.HasPendingWorld || _migration.Failure != null)
                {
                    throw new InvalidOperationException(
                        "No valid world snapshot was received. The match will not be reset."
                    );
                }

                CurrentRoom = new RoomConnectionSettings(
                    CurrentRoom.RoomCode,
                    CurrentRoom.MaxPlayers,
                    _migration.Pending.Difficulty
                );
                if (!LoadNetworkScene(SceneNames.Game))
                {
                    throw new InvalidOperationException(Status);
                }
            }
            float deadline = Time.realtimeSinceStartup + 40f;
            while (!_shutdownRequested &&
                (State != NetworkSessionState.InGame ||
                    _migration.HasPendingWorld ||
                    _networkManager.LocalClient?.PlayerObject == null))
            {
                if (_migration.Failure != null || Time.realtimeSinceStartup > deadline)
                {
                    throw new TimeoutException(_migration.Failure ?? "World restoration timed out.");
                }

                await Awaitable.NextFrameAsync();
            }
            if (_shutdownRequested)
            {
                throw new OperationCanceledException();
            }

            _migration.CompleteRecovery();
            SetState(NetworkSessionState.InGame, "Match restored");
        }

        private async Awaitable UploadFinalMigrationSnapshotAsync()
        {
            if (_finalUpload ||
                Mode != NetworkSessionMode.Host ||
                !_networkManager.IsServer ||
                _sessionConnector.ActiveSession is not { PlayerCount: > 1 } session)
            {
                return;
            }

            _finalUpload = true;
            try
            {
                _migration.BeginRecovery();
                ++_uploadGeneration;
                if (_migration.PausePeers != null)
                {
                    await _migration.PausePeers();
                }
                if (_snapshotUpload != null)
                {
                    try
                    {
                        await _snapshotUpload;
                    }
                    catch (Exception) { }
                }
                byte[] data = _migration.Capture();
                if (data.Length > 0)
                {
                    await session.AsHost().SetHostMigrationDataAsync(data, TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning(
                    "Final migration upload failed; recovery will use the latest periodic snapshot: " + error.Message
                );
            }
            finally
            {
                _finalUpload = false;
            }
        }

        private void ApproveMigratingConnection(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response
        )
        {
            _approvalHandler.ApproveRoomConnection(request, response, CurrentRoom);
            if (!response.Approved || request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                return;
            }

            if (!ConnectionApprovalHandler.TryDeserializePayload(request.Payload, out var payload) ||
                string.IsNullOrEmpty(payload.PlayerId) ||
                payload.PlayerId.Length > 128 ||
                (_sessionConnector.ActiveSession != null &&
                    !_sessionConnector.ActiveSession.Players.Any(p => p.Id == payload.PlayerId)) ||
                _migration.Clients.Any(p =>
                    p.Value == payload.PlayerId && _networkManager.ConnectedClients.ContainsKey(p.Key)
                ))
            {
                ConnectionApprovalHandler.Reject(response, SessionFailureReasons.InvalidPayload);
                return;
            }
            _migration.Clients[request.ClientNetworkId] = payload.PlayerId;
        }
    }
}
