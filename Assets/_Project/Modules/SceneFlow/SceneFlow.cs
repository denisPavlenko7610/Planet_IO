using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityTemplates.SceneFlow
{
	public sealed class SceneFlow : ISceneFlow
	{
		private const float SceneLoadReadyProgress = 0.9f;

		private readonly SceneCatalog _catalog;
		private readonly ISceneTransition _transition;
		private readonly ISceneOperationScope _operationScope;

		private bool _isBusy;
		private float _progress;

		public SceneFlow(SceneCatalog catalog)
			: this(catalog, EmptyTransition.Instance, EmptyOperationScope.Instance) { }

		public SceneFlow(SceneCatalog catalog, ISceneTransition transition)
			: this(catalog, transition, EmptyOperationScope.Instance) { }

		public SceneFlow(SceneCatalog catalog, ISceneTransition transition, ISceneOperationScope operationScope)
		{
			_catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
			_transition = transition ?? throw new ArgumentNullException(nameof(transition));
			_operationScope = operationScope ?? throw new ArgumentNullException(nameof(operationScope));
			_catalog.ThrowIfInvalid();
		}

		public bool IsBusy => _isBusy;

		public float Progress => _progress;

		public event Action<float> ProgressChanged;

		public async Awaitable ChangeSceneAsync(string sceneId, CancellationToken cancellationToken = default)
		{
			await SwitchToMainThreadAsync(cancellationToken);

			EnsureNotBusy();

			string scenePath = _catalog.GetPath(sceneId);

			if (IsSceneSingleActive(scenePath))
			{
				return;
			}

			await ChangeSceneInternalAsync(scenePath, cancellationToken);
		}

		public async Awaitable ReloadActiveSceneAsync(CancellationToken cancellationToken = default)
		{
			await SwitchToMainThreadAsync(cancellationToken);

			EnsureNotBusy();

			Scene activeScene = SceneManager.GetActiveScene();

			if (!activeScene.IsValid())
			{
				throw new InvalidOperationException("There is no valid active scene.");
			}

			if (string.IsNullOrWhiteSpace(activeScene.path))
			{
				throw new InvalidOperationException("The active scene does not have a valid asset path.");
			}

			await ChangeSceneInternalAsync(activeScene.path, cancellationToken);
		}

		public async Awaitable LoadAdditiveAsync(
			string sceneId,
			bool setActive = false,
			CancellationToken cancellationToken = default
		)
		{
			await SwitchToMainThreadAsync(cancellationToken);

			EnsureNotBusy();

			string scenePath = _catalog.GetPath(sceneId);

			Scene loadedScene = SceneManager.GetSceneByPath(scenePath);

			if (loadedScene.IsValid() && loadedScene.isLoaded)
			{
				if (setActive)
				{
					SetActiveScene(loadedScene);
				}

				return;
			}

			cancellationToken.ThrowIfCancellationRequested();

			await RunSceneOperationAsync(
				() => LoadSceneInternalAsync(scenePath, LoadSceneMode.Additive, setActive)
			);
		}

		public async Awaitable UnloadAsync(
			string sceneId,
			string fallbackActiveSceneId = null,
			CancellationToken cancellationToken = default
		)
		{
			await SwitchToMainThreadAsync(cancellationToken);

			EnsureNotBusy();

			string scenePath = _catalog.GetPath(sceneId);

			Scene scene = SceneManager.GetSceneByPath(scenePath);

			if (!scene.IsValid() || !scene.isLoaded)
			{
				return;
			}

			cancellationToken.ThrowIfCancellationRequested();

			await RunSceneOperationAsync(async () =>
				{
					if (scene == SceneManager.GetActiveScene())
					{
						SetFallbackActiveScene(sceneId, fallbackActiveSceneId);
					}

					AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);

					if (operation == null)
					{
						throw new InvalidOperationException($"Failed to unload scene '{sceneId}'.");
					}

					await TrackOperationProgressAsync(operation);
				}
			);
		}

		public void SetActive(string sceneId)
		{
			EnsureNotBusy();

			string scenePath = _catalog.GetPath(sceneId);

			Scene scene = SceneManager.GetSceneByPath(scenePath);

			if (!scene.IsValid() || !scene.isLoaded)
			{
				throw new InvalidOperationException($"Scene '{sceneId}' is not loaded.");
			}

			SetActiveScene(scene);
		}

		public bool IsLoaded(string sceneId)
		{
			string scenePath = _catalog.GetPath(sceneId);

			Scene scene = SceneManager.GetSceneByPath(scenePath);

			return scene.IsValid() && scene.isLoaded;
		}

		private async Awaitable ChangeSceneInternalAsync(string scenePath, CancellationToken cancellationToken)
		{
			await RunSceneOperationAsync(async () =>
				{
					bool transitionStarted = false;

					try
					{
						transitionStarted = true;

						await _transition.CoverAsync(cancellationToken);

						cancellationToken.ThrowIfCancellationRequested();

						await LoadSceneInternalAsync(scenePath, LoadSceneMode.Single, setActive: true);
					}
					finally
					{
						if (transitionStarted)
						{
							await _transition.RevealAsync(CancellationToken.None);
						}
					}
				}
			);
		}

		private async Awaitable LoadSceneInternalAsync(string scenePath, LoadSceneMode loadMode, bool setActive)
		{
			AsyncOperation operation = SceneManager.LoadSceneAsync(scenePath, loadMode);

			if (operation == null)
			{
				throw new InvalidOperationException($"Failed to start loading scene '{scenePath}'.");
			}

			await TrackSceneLoadProgressAsync(operation);

			Scene loadedScene = SceneManager.GetSceneByPath(scenePath);

			if (!loadedScene.IsValid() || !loadedScene.isLoaded)
			{
				throw new InvalidOperationException($"Scene '{scenePath}' was not loaded correctly.");
			}

			if (setActive)
			{
				SetActiveScene(loadedScene);
			}
		}

		private async Awaitable TrackSceneLoadProgressAsync(AsyncOperation operation)
		{
			while (!operation.isDone)
			{
				float normalizedProgress = operation.progress / SceneLoadReadyProgress;

				SetProgress(normalizedProgress);

				await Awaitable.NextFrameAsync();
			}

			SetProgress(1f);
		}

		private async Awaitable TrackOperationProgressAsync(AsyncOperation operation)
		{
			while (!operation.isDone)
			{
				SetProgress(operation.progress);

				await Awaitable.NextFrameAsync();
			}

			SetProgress(1f);
		}

		private void SetFallbackActiveScene(string sceneId, string fallbackSceneId)
		{
			if (string.IsNullOrWhiteSpace(fallbackSceneId))
			{
				throw new InvalidOperationException(
					$"Scene '{sceneId}' is active. Provide a fallback active scene before unloading it.");
			}

			string fallbackPath = _catalog.GetPath(fallbackSceneId);
			Scene fallbackScene = SceneManager.GetSceneByPath(fallbackPath);

			if (!fallbackScene.IsValid() || !fallbackScene.isLoaded)
			{
				throw new InvalidOperationException($"Fallback scene '{fallbackSceneId}' is not loaded.");
			}

			SetActiveScene(fallbackScene);
		}

		private static void SetActiveScene(Scene scene)
		{
			if (!SceneManager.SetActiveScene(scene))
			{
				throw new InvalidOperationException($"Failed to set scene '{scene.path}' as active.");
			}
		}

		private static bool IsSceneSingleActive(string scenePath)
		{
			if (SceneManager.sceneCount != 1)
			{
				return false;
			}

			Scene activeScene = SceneManager.GetActiveScene();

			return activeScene.IsValid()
				&& activeScene.isLoaded
				&& string.Equals(activeScene.path, scenePath, StringComparison.OrdinalIgnoreCase);
		}

		private static async Awaitable SwitchToMainThreadAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			await Awaitable.MainThreadAsync();

			cancellationToken.ThrowIfCancellationRequested();
		}

		private async Awaitable RunSceneOperationAsync(Func<Awaitable> operation)
		{
			BeginOperation();

			try
			{
				using (_operationScope.Enter())
				{
					await operation();
				}
			}
			finally
			{
				EndOperation();
			}
		}

		private void BeginOperation()
		{
			EnsureNotBusy();
			_isBusy = true;
			SetProgress(0f);
		}

		private void EndOperation()
		{
			_isBusy = false;
		}

		private void EnsureNotBusy()
		{
			if (_isBusy)
			{
				throw new InvalidOperationException("Another scene operation is already running.");
			}
		}

		private void SetProgress(float progress)
		{
			_progress = Mathf.Clamp01(progress);
			ProgressChanged?.Invoke(_progress);
		}

		private sealed class EmptyTransition : ISceneTransition
		{
			public static readonly EmptyTransition Instance = new();

			public Awaitable CoverAsync(CancellationToken cancellationToken = default) => Complete(cancellationToken);

			public Awaitable RevealAsync(CancellationToken cancellationToken = default) => Complete(cancellationToken);

			private static Awaitable Complete(CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				AwaitableCompletionSource source = new();
				Awaitable awaitable = source.Awaitable;
				source.SetResult();
				return awaitable;
			}
		}

		private sealed class EmptyOperationScope : ISceneOperationScope
		{
			public static readonly EmptyOperationScope Instance = new();

			public IDisposable Enter() => EmptyDisposable.Instance;
		}

		private sealed class EmptyDisposable : IDisposable
		{
			public static readonly EmptyDisposable Instance = new();

			public void Dispose() { }
		}
	}
}
