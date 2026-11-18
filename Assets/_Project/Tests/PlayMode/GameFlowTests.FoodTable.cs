using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
	public sealed partial class GameFlowTests
	{
		[UnityTest, Timeout(45000)]
		public IEnumerator FoodTable_CreatesTwoThousandRecordsWithoutFoodNetworkObjectsOrColliders()
		{
			yield return StartMoonGame();
			Assert.That(AllFood().Count(food => !food.IsDropped), Is.EqualTo(2000));
			Assert.That(NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count, Is.LessThan(100));
			Assert.That(Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude)
				.Count(collider => collider.gameObject.layer == LayerMask.NameToLayer(Constants.FoodLayerName)), Is.Zero);
			Player player = FindLocalPlayer();
			FoodRecord visible = AllFood().First(food => !food.IsDropped);
			visible.Position = (Vector2)player.transform.position + Vector2.up * 2f;
			yield return null;
			yield return null;
			SpriteRenderer[] views = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude)
				.Where(renderer => renderer.name == "FoodView").ToArray();
			Assert.That(views.Length, Is.GreaterThan(0));
			Assert.That(views.Length, Is.LessThan(2000), "Only the camera region should create active food views.");
			foreach (SpriteRenderer view in views)
			{
				Assert.That(view.GetComponent<NetworkObject>(), Is.Null);
				Assert.That(view.GetComponent<Collider2D>(), Is.Null);
			}
			CaptureFoodRendering();
		}

		private static void CaptureFoodRendering()
		{
			Camera camera = Camera.main;
			Assert.That(camera, Is.Not.Null);
			var target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
			var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
			RenderTexture previous = RenderTexture.active;
			try
			{
				UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
					new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
				RenderTexture.active = target;
				pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
				pixels.Apply();
				System.IO.Directory.CreateDirectory("Logs");
				System.IO.File.WriteAllBytes("Logs/FoodData.Game.png", pixels.EncodeToPNG());
			}
			finally
			{
				RenderTexture.active = previous;
				Object.Destroy(pixels);
				target.Release();
				Object.Destroy(target);
			}
		}

		[UnityTest, Timeout(45000)]
		public IEnumerator FoodTable_MagnetMovesFoodButRespectsDropperImmunityAndPickup()
		{
			yield return StartMoonGame();
			Player player = PrepareMoonPlayer();
			FoodSpawner spawner = Object.FindFirstObjectByType<FoodSpawner>();
			FoodRecord food = AllFood().First(item => !item.IsDropped);
			food.Capacity = 0.1f;
			food.Position = (Vector2)player.transform.position + Vector2.right * (player.BodyRadius + 0.15f);
			GameObject marker = new("Food drop marker");
			try
			{
				marker.transform.position = food.Position;
				FoodRecord immune = spawner.SpawnAt(marker.transform, 0.05f);
				immune.SetDropper(player.NetworkObjectId, 5f);
				Vector2 before = food.Position, protectedBefore = immune.Position;
				typeof(Player).GetMethod("AttractFood", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
				Assert.That(Vector2.Distance(food.Position, player.transform.position), Is.LessThan(Vector2.Distance(before, player.transform.position)));
				Assert.That(immune.Position, Is.EqualTo(protectedBefore));
				food.Position = player.transform.position;
				float mass = player.Capacity;
				ulong oldHandle = food.Id;
				RunFoodSweep(player);
				Assert.That(player.Capacity, Is.GreaterThan(mass));
				Assert.That(spawner.World.Find(oldHandle), Is.Null);
				Assert.That(AllFood().Count(item => !item.IsDropped), Is.EqualTo(2000));
			}
			finally { Object.Destroy(marker); }
		}
	}
}
