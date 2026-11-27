using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PlanetIO.Application;
using PlanetIO.Core.Contracts.Loading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed class GameFlowServiceTests
    {
        private sealed class LoadingViewSpy : IGameLoadingView
        {
            public int ShowCount { get; private set; }
            public int HideCount { get; private set; }

            public void Show() => ShowCount++;

            public void Hide() => HideCount++;
        }

        private readonly List<(GameState Previous, GameState Next)> _transitions = new();
        private GameObject _root;
        private LoadingViewSpy _loadingView;
        private GameFlowService _service;

        [SetUp]
        public void SetUp()
        {
            _transitions.Clear();
            _root = new GameObject("GameFlowServiceTests");
            _loadingView = new LoadingViewSpy();
            _service = new GameFlowService(
                _root.AddComponent<FoodSpawner>(),
                _root.AddComponent<CometSpawner>(),
                _root.AddComponent<EnemySpawner>(),
                _root.AddComponent<FoodPool>(),
                _root.AddComponent<CometPool>(),
                _root.AddComponent<EnemyPool>(),
                _root.AddComponent<NetworkManager>(),
                _root.AddComponent<NetworkWorldReadyState>(),
                _loadingView);
            _service.StateChanged += (previous, next) => _transitions.Add((previous, next));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator StartAsync_WithoutListeningNetwork_FailsStartupAndShutsDown()
        {
            yield return AwaitableTestRunner.Run(_service.StartAsync());

            Assert.That(_service.State, Is.EqualTo(GameState.ShuttingDown));
            Assert.That(_service.IsGameplayActive, Is.False);
            Assert.That(_loadingView.ShowCount, Is.EqualTo(1));
            Assert.That(_loadingView.HideCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(
                _transitions,
                Is.EqualTo(new[]
                {
                    (GameState.None, GameState.Initializing),
                    (GameState.Initializing, GameState.ShuttingDown)
                }));
        }

        [Test]
        public void Dispose_ShutsDownOnceAndHidesLoading()
        {
            _service.Dispose();
            _service.Dispose();

            Assert.That(_service.State, Is.EqualTo(GameState.ShuttingDown));
            Assert.That(_service.IsGameplayActive, Is.False);
            Assert.That(_transitions, Has.Count.EqualTo(1));
            Assert.That(_loadingView.HideCount, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void BeforeStart_TickAndFinishGameDoNotChangeState()
        {
            _service.Tick();
            _service.FinishGame();

            Assert.That(_service.State, Is.EqualTo(GameState.None));
            Assert.That(_service.IsGameplayActive, Is.False);
            Assert.That(_transitions, Is.Empty);
        }
    }
}
