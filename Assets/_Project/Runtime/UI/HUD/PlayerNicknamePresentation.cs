using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public interface IPlayerNicknameView
    {
        void ShowNickname(string nickname);
        void Dispose();
    }

    public sealed class PlayerNicknameView : MonoBehaviour, IPlayerNicknameView
    {
        private const float VerticalOffset = 0.35f;
        private const float PlayerVisualRadius = 3.55f;

        private Player _player;
        private TextMeshPro _nicknameText;

        public static PlayerNicknameView Create(Player player)
        {
            GameObject viewObject = new($"Nickname View ({player.OwnerClientId})");
            PlayerNicknameView view = viewObject.AddComponent<PlayerNicknameView>();
            view.Initialize(player);
            return view;
        }

        public void ShowNickname(string nickname)
        {
            _nicknameText.text = NicknameRules.Normalize(nickname);
        }

        public void Dispose()
        {
            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        private void LateUpdate()
        {
            if (_player == null || !_player.IsSpawned)
            {
                return;
            }

            float height = _player.Capacity * PlayerVisualRadius + VerticalOffset;
            transform.position = _player.transform.position + Vector3.up * height;
        }

        private void Initialize(Player player)
        {
            _player = player
				? player
                : throw new ArgumentNullException(nameof(player));

            _nicknameText = gameObject.AddComponent<TextMeshPro>();
            _nicknameText.alignment = TextAlignmentOptions.Center;
            _nicknameText.textWrappingMode = TextWrappingModes.NoWrap;
            _nicknameText.fontSize = 2.2f;
            _nicknameText.color = Color.white;
            _nicknameText.rectTransform.sizeDelta = new Vector2(8f, 1.2f);
            _nicknameText.renderer.sortingOrder = 100;
            transform.localScale = Vector3.one * 0.28f;
        }
    }

    public sealed class PlayerNicknamePresenter : ITickable, IDisposable
    {
        private const float RefreshIntervalSeconds = 0.25f;

        private readonly NetworkManager _networkManager;
        private readonly Dictionary<PlayerNickname, NicknameBinding> _viewsByNickname = new();
        private readonly List<PlayerNickname> _nicknamesToRemove = new();
        private float _refreshTimeRemaining;

        public PlayerNicknamePresenter(NetworkManager networkManager)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
        }

        public void Tick()
        {
            _refreshTimeRemaining -= Time.unscaledDeltaTime;
            if (_refreshTimeRemaining > 0f)
            {
                return;
            }

            _refreshTimeRemaining = RefreshIntervalSeconds;
            BindSpawnedPlayers();
            RemoveDespawnedPlayers();
        }

        public void Dispose()
        {
            foreach (KeyValuePair<PlayerNickname, NicknameBinding> binding in _viewsByNickname)
            {
                if (binding.Key != null)
                {
                    binding.Key.NicknameChanged -= binding.Value.Handler;
                }

                binding.Value.View.Dispose();
            }

            _viewsByNickname.Clear();
            _nicknamesToRemove.Clear();
        }

        private void BindSpawnedPlayers()
        {
            if (_networkManager.SpawnManager == null)
            {
                return;
            }

            foreach (NetworkObject networkObject in _networkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!networkObject.IsPlayerObject ||
                    !networkObject.TryGetComponent(out PlayerNickname playerNickname) ||
                    _viewsByNickname.ContainsKey(playerNickname) ||
                    !networkObject.TryGetComponent(out Player player))
                {
                    continue;
                }

                IPlayerNicknameView nicknameView = PlayerNicknameView.Create(player);
                Action<string> nicknameChangedHandler = nicknameView.ShowNickname;
                _viewsByNickname.Add(playerNickname, new NicknameBinding(nicknameView, nicknameChangedHandler));
                playerNickname.NicknameChanged += nicknameChangedHandler;
                nicknameView.ShowNickname(playerNickname.Nickname);
            }
        }

        private void RemoveDespawnedPlayers()
        {
            _nicknamesToRemove.Clear();

            foreach (PlayerNickname playerNickname in _viewsByNickname.Keys)
            {
                if (playerNickname == null || !playerNickname.IsSpawned)
                {
                    _nicknamesToRemove.Add(playerNickname);
                }
            }

            foreach (PlayerNickname playerNickname in _nicknamesToRemove)
            {
                NicknameBinding binding = _viewsByNickname[playerNickname];

                if (playerNickname != null)
                {
                    playerNickname.NicknameChanged -= binding.Handler;
                }

                binding.View.Dispose();
                _viewsByNickname.Remove(playerNickname);
            }
        }

        private sealed class NicknameBinding
        {
            public NicknameBinding(IPlayerNicknameView view, Action<string> handler)
			{
                View = view;
                Handler = handler;
            }

            public IPlayerNicknameView View { get; }
            public Action<string> Handler { get; }
        }
    }
}
