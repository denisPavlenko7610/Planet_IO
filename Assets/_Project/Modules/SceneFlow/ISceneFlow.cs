using System;
using System.Threading;
using UnityEngine;

namespace UnityTemplates.SceneFlow
{
	public interface ISceneFlow
	{
		bool IsBusy { get; }

		float Progress { get; }

		event Action<float> ProgressChanged;

		Awaitable ChangeSceneAsync(string sceneId, CancellationToken cancellationToken = default);

		Awaitable ReloadActiveSceneAsync(CancellationToken cancellationToken = default);

		Awaitable LoadAdditiveAsync(
			string sceneId,
			bool setActive = false,
			CancellationToken cancellationToken = default
		);

		Awaitable UnloadAsync(
			string sceneId,
			string fallbackActiveSceneId = null,
			CancellationToken cancellationToken = default
		);

		void SetActive(string sceneId);

		bool IsLoaded(string sceneId);
	}
}
