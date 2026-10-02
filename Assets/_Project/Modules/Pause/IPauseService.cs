using System;
using System.Collections.Generic;

namespace UnityTemplates.Pause
{
	public interface IPauseService
	{
		bool IsPaused { get; }

		IReadOnlyList<string> ActiveReasons { get; }

		event Action<PauseChanged> Changed;

		IDisposable RequestPause(string reason);
	}
}
