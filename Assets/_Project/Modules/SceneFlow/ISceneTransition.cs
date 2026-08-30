using System.Threading;
using UnityEngine;

namespace UnityTemplates.SceneFlow
{
	public interface ISceneTransition
	{
		Awaitable CoverAsync(CancellationToken cancellationToken = default);

		Awaitable RevealAsync(CancellationToken cancellationToken = default);
	}
}
