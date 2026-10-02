using System;
using UnityEngine;

namespace UnityTemplates.Pause
{
	public sealed class UnityTimeScalePauseController : IDisposable
	{
		private readonly IPauseService _pauseService;

		private float _resumeTimeScale;
		private bool _pauseApplied;

		public UnityTimeScalePauseController(IPauseService pauseService)
		{
			_pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));

			_pauseService.Changed += OnPauseChanged;

			if (_pauseService.IsPaused)
			{
				ApplyPause();
			}
		}

		public void Dispose()
		{
			_pauseService.Changed -= OnPauseChanged;
			RemovePause();
		}

		private void OnPauseChanged(PauseChanged change)
		{
			if (change.IsPaused)
			{
				ApplyPause();
			}
			else
			{
				RemovePause();
			}
		}

		private void ApplyPause()
		{
			if (_pauseApplied)
			{
				return;
			}

			_resumeTimeScale = Time.timeScale;
			Time.timeScale = 0f;
			_pauseApplied = true;
		}

		private void RemovePause()
		{
			if (!_pauseApplied)
			{
				return;
			}

			Time.timeScale = _resumeTimeScale;
			_pauseApplied = false;
		}
	}
}
