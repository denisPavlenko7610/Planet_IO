using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using PlanetIO.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class SharedWorldAttribute : OrderAttribute
	{
		public SharedWorldAttribute() : base(1)
		{
		}
	}

	public sealed partial class GameFlowTests
	{
		private static bool _sharedWorldActive;
		private static bool _sharedWorldInUse;

		private static IEnumerator StartSharedMoonGame()
		{
			if (_sharedWorldActive && SharedWorldAlive())
			{
				yield return ResetSharedWorld();
			}
			else
			{
				yield return EndSharedWorld();
				yield return StartMoonGame();
				_sharedWorldActive = true;
			}

			_sharedWorldInUse = true;
		}

		private static IEnumerator EndSharedWorld()
		{
			if (!_sharedWorldActive)
			{
				yield break;
			}

			_sharedWorldActive = false;
			_sharedWorldInUse = false;
			yield return ShutdownSession();
		}

		private static bool KeepSharedWorld()
		{
			bool keep = _sharedWorldActive &&
				_sharedWorldInUse &&
				TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed;
			_sharedWorldInUse = false;
			return keep;
		}

		private static bool SharedWorldAlive()
		{
			GameLifetimeScope scope = Object.FindAnyObjectByType<GameLifetimeScope>();
			return scope != null &&
				scope.Container != null &&
				SceneManager.GetActiveScene().name == SceneNames.Game &&
				FindLocalPlayer() != null &&
				scope.Container.Resolve<IGameStateService>().IsGameplayActive;
		}

		private static IEnumerator ResetSharedWorld()
		{
			Rect bounds = WorldBounds.Area;
			Vector2 junkyard = new(bounds.xMax - 10f, bounds.yMax - 10f);
			Player player = FindLocalPlayer();
			Park(player.transform, junkyard);
			if (!player.IsDefeated)
			{
				player.Defeat();
			}

			player.RespawnRpc();
			player.GetComponent<PlayerMovement>().enabled = true;
			PlanetGravity gravity = player.GetComponent<PlanetGravity>();
			gravity.enabled = false;
			gravity.enabled = true;
			ViewBounds.Clear();

			var enemies = ResolveGame<IRespawnService<Enemy>>();
			foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
			{
				Park(enemy.transform, junkyard);
				enemies.Respawn(enemy);
				if (enemy.TryGetComponent(out EnemyMovement movement))
				{
					movement.enabled = true;
				}
			}

			var comets = ResolveGame<IRespawnService<Comet>>();
			foreach (Comet comet in Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude))
			{
				comets.Respawn(comet);
			}

			FoodSpawner food = Object.FindFirstObjectByType<FoodSpawner>();
			var leftovers = new List<FoodRecord>();
			food.Query(new Vector2(-40f, -40f), 20f, leftovers);
			foreach (FoodRecord leftover in leftovers)
			{
				food.Respawn(leftover);
			}

			Physics2D.SyncTransforms();
			yield return new WaitForSeconds(0.2f);
		}

		private static void Park(Transform body, Vector2 position)
		{
			body.position = position;
			if (body.TryGetComponent(out Rigidbody2D rigidbody))
			{
				rigidbody.position = position;
				rigidbody.linearVelocity = Vector2.zero;
			}

			Physics2D.SyncTransforms();
		}

		[UnityOneTimeTearDown]
		public IEnumerator EndSharedWorldAfterFixture()
		{
			yield return EndSharedWorld();
			RD.Settings.ISettingsService settings = Resolve<RD.Settings.ISettingsService>();
			settings.Set(GameSettingKeys.TutorialCompleted, _tutorialCompletedBeforeTests);
			settings.Flush();
		}
	}
}
