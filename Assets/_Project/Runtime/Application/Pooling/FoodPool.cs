using UnityEngine;

namespace PlanetIO
{
	// Scene configuration for the data table and shared client views; never creates food entities.
	public sealed class FoodPool : MonoBehaviour
	{
		[SerializeField, Min(1)] private int _capacity = 2000;
		[SerializeField] private Sprite _sprite;
		[SerializeField] private Material _material;
		public int Capacity => Mathf.Max(1, _capacity);
		public Sprite Sprite => _sprite;
		public Material Material => _material;
		public static readonly Color[] Colors =
		{
			new(0.5411765f, 0.91764706f, 0.96862745f), new(0.9843137f, 0.39607844f, 0.3764706f),
			new(0.99215686f, 0.2f, 0.2f), new(0.8235294f, 0.4117647f, 0.91764706f)
		};
	}
}
