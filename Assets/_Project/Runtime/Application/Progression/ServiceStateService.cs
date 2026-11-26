using System;
using System.IO;
using System.Threading;
using RD.Foundation;
using RD.Save;
using UnityEngine;

namespace PlanetIO.Application
{
    public sealed class ServiceStateService : IServiceStateService
    {
        private const string Slot = "service-state";

        private readonly SaveStore _saves;
        private ServiceState _state = new();
        private bool _initialized;
        private bool _saving;
        private bool _dirty;

        public ServiceStateService(SaveStore saves)
        {
            _saves = saves ?? throw new ArgumentNullException(nameof(saves));
        }

        public event Action Changed;

        public bool IsLoaded => _initialized;

        public ServiceState State => _state;

        public async Awaitable InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                _state = await _saves.LoadAsync<ServiceState>(Slot, Validate, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                _state = new ServiceState();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                UnityLogger.LogException(exception);
                _state = new ServiceState();
            }

            _initialized = true;
            Changed?.Invoke();
        }

        public void MarkDirty()
        {
            if (!_initialized)
            {
                return;
            }

            _dirty = true;
            Changed?.Invoke();
            if (!_saving)
            {
                _ = SaveLoopAsync();
            }
        }

        public async Awaitable FlushAsync()
        {
            if (!_initialized || !_dirty)
            {
                return;
            }

            _dirty = false;
            await _saves.SaveAsync(Slot, _state);
        }

        private static void Validate(ServiceState state)
        {
            if (state == null || !state.IsValid())
            {
                throw new InvalidSaveDataException("Unsupported or invalid Planet_IO service state.");
            }
        }

        private async Awaitable SaveLoopAsync()
        {
            _saving = true;
            try
            {
                while (_dirty)
                {
                    _dirty = false;
                    await _saves.SaveAsync(Slot, _state);
                }
            }
            catch (Exception exception)
            {
                UnityLogger.LogException(exception);
            }
            finally
            {
                _saving = false;
            }
        }
    }
}
