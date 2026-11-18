using System.Reflection;
using NUnit.Framework;
using PlanetIO.UI.Hud;
using UnityEditor;
using UnityEngine;

namespace PlanetIO.Tests
{
	public sealed class DirectionChevronTests
	{
		[TestCase(0.08f, 3.6f)]
		[TestCase(0.25f, 7f)]
		[TestCase(0.8f, 13f)]
		[TestCase(0.8f, 14.04f)]
		public void PlayerPrefab_ChevronKeepsScreenSizeAndClearsBody(float scale, float zoom)
		{
			GameObject holder = new GameObject("Chevron test holder");
			holder.SetActive(false);
			GameObject cameraObject = new GameObject("Chevron test camera");
			try
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
				GameObject instance = Object.Instantiate(prefab, holder.transform);
				instance.transform.localPosition = Vector3.zero;
				instance.transform.localScale = new Vector3(scale, scale, 1f);
				PlayerDirectionArrow arrow = instance.GetComponentInChildren<PlayerDirectionArrow>(true);
				Assert.That(arrow, Is.Not.Null, "The player prefab must wire the direction indicator.");
				Invoke(arrow, "Awake");
				Camera camera = cameraObject.AddComponent<Camera>();
				camera.orthographic = true;
				camera.orthographicSize = zoom;
				typeof(PlayerDirectionArrow).GetField("_camera", BindingFlags.NonPublic | BindingFlags.Instance)
					.SetValue(arrow, camera);
				arrow.Bind(instance.GetComponent<PlayerMovement>());
				arrow.Show();
				Invoke(arrow, "LateUpdate");

				SpriteRenderer marker = instance.transform.Find("Arrow").GetComponent<SpriteRenderer>();
				Assert.That(marker.sprite, Is.SameAs(OverlaySprites.Chevron));
				Assert.That(marker.enabled, Is.True);
				float worldSize = marker.sprite.bounds.size.y * marker.transform.lossyScale.y;
				Assert.That(worldSize / (2f * zoom), Is.EqualTo(0.018f).Within(1e-5f));
				Assert.That(marker.color.a, Is.EqualTo(0.75f).Within(1e-5f));
				Assert.That(Vector2.Dot(marker.transform.right, Vector2.right), Is.GreaterThan(0.999f));
				float radius = instance.GetComponent<Player>().BodyRadius;
				float distance = Vector2.Distance(marker.transform.position, instance.transform.position);
				Assert.That(distance - worldSize * 0.5f, Is.GreaterThanOrEqualTo(radius * 1.12f + worldSize * 0.49f));
			}
			finally
			{
				Object.DestroyImmediate(holder);
				Object.DestroyImmediate(cameraObject);
			}
		}

		[TestCase(0.5f, 0.8f)]
		[TestCase(0.8f, 0.5f)]
		public void Clearance_AccountsForSpriteAndCollider(float bodyRadius, float spriteRadius)
		{
			float size = DirectionArrowRules.WorldSize(3.6f, DirectionArrowRules.ScreenHeightFraction);
			float radius = DirectionArrowRules.ClearanceRadius(bodyRadius, spriteRadius);
			float distance = DirectionArrowRules.Distance(radius, size, 0.5f);
			Assert.That(distance - size * 0.5f, Is.GreaterThan(Mathf.Max(bodyRadius, spriteRadius)));
		}

		private static void Invoke(PlayerDirectionArrow arrow, string method)
		{
			typeof(PlayerDirectionArrow).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)
				.Invoke(arrow, null);
		}
	}
}
