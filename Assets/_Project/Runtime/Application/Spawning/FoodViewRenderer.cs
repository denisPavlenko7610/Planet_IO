using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
	public sealed class FoodViewRenderer : IDisposable
	{
		private sealed class View
		{
			public SpriteRenderer Renderer;
			public ulong Id;
			public int Stamp;
		}
		private readonly FoodPool _configuration;
		private readonly Transform _root;
		private readonly Dictionary<int, View> _visible = new();
		private readonly Stack<View> _pool = new();
		private readonly List<FoodRecord> _query = new(512);
		private readonly List<int> _hidden = new(512);
		private int _stamp;
		private Camera _camera;
		public FoodViewRenderer(FoodPool configuration, Transform root) { _configuration = configuration; _root = root; }
		public void Render(FoodWorld world, float deltaTime)
		{
			if (world == null || _configuration.Sprite == null) return;
			_camera ??= Camera.main;
			if (_camera == null) return;
			_stamp++;
			Vector2 center = _camera.transform.position;
			float height = _camera.orthographicSize + 4f, width = _camera.orthographicSize * _camera.aspect + 4f;
			Rect rect = new(center.x - width, center.y - height, width * 2f, height * 2f);
			world.QueryRect(rect, _query);
			float blend = 1f - Mathf.Exp(-deltaTime / 0.06f);
			foreach (FoodRecord food in _query)
			{
				int slot = FoodWorld.SlotOf(food.Id);
				if (!_visible.TryGetValue(slot, out View view))
				{
					view = _pool.Count > 0 ? _pool.Pop() : Create();
					view.Id = 0;
					view.Renderer.gameObject.SetActive(true);
					_visible.Add(slot, view);
				}
				view.Stamp = _stamp;
				Transform transform = view.Renderer.transform;
				Vector3 target = new(food.Position.x, food.Position.y, 0f);
				Vector3 position = transform.position;
				bool snap = world.Authoritative || view.Id != food.Id || (position - target).sqrMagnitude > 4f;
				Vector3 next = snap ? target : Vector3.Lerp(position, target, blend);
				if (position != next) transform.position = next;
				view.Id = food.Id;
				Vector3 scale = new(food.Capacity * FoodViewScale.Factor, food.Capacity * FoodViewScale.Factor, 1f);
				if (transform.localScale != scale) transform.localScale = scale;
				Color color = food.ValueMultiplier > 1f ? new Color(1f, 0.82f, 0.25f) : FoodPool.Colors[food.Flavor % FoodPool.Colors.Length];
				if (view.Renderer.color != color) view.Renderer.color = color;
			}
			_hidden.Clear();
			foreach (var pair in _visible) if (pair.Value.Stamp != _stamp) _hidden.Add(pair.Key);
			foreach (int slot in _hidden)
			{
				View view = _visible[slot];
				view.Renderer.gameObject.SetActive(false);
				_pool.Push(view);
				_visible.Remove(slot);
			}
		}
		private View Create()
		{
			var obj = new GameObject("FoodView");
			obj.transform.SetParent(_root, false);
			SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
			renderer.sprite = _configuration.Sprite;
			renderer.sharedMaterial = _configuration.Material;
			renderer.sortingLayerID = -768061997;
			return new View { Renderer = renderer };
		}
		public void Dispose()
		{
			foreach (var pair in _visible) if (pair.Value.Renderer != null) UnityEngine.Object.Destroy(pair.Value.Renderer.gameObject);
			foreach (View view in _pool) if (view.Renderer != null) UnityEngine.Object.Destroy(view.Renderer.gameObject);
			_visible.Clear(); _pool.Clear();
		}
	}
}
