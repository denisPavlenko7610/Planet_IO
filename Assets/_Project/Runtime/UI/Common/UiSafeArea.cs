using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Screen = UnityEngine.Device.Screen;

namespace PlanetIO.UI
{
	[DefaultExecutionOrder(100)]
	[RequireComponent(typeof(UIDocument))]
	public sealed class UiSafeArea : MonoBehaviour
	{
		[SerializeField] private string _elementName = "safe-area";

		private VisualElement _root;
		private List<VisualElement> _targets;
		private Rect _applied;
		private Vector2 _appliedPanelSize;
		private Vector2Int _appliedScreenSize;

		private void OnEnable()
		{
			_root = GetComponent<UIDocument>().rootVisualElement;
			_targets = _root.Query<VisualElement>(className: "safe-modal").ToList();
			VisualElement mainArea = _root.Q<VisualElement>(_elementName);
			if (mainArea != null)
			{
				_targets.Add(mainArea);
			}

			_appliedPanelSize = default;
			Apply();
		}

		private void LateUpdate()
		{
			Apply();
		}

		private void Apply()
		{
			IPanel panel = _root.panel;
			Vector2 panelSize = _root.layout.size;
			if (panel == null || !(panelSize.x > 0f && panelSize.y > 0f))
			{
				return;
			}

			Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
			Rect safe = Screen.safeArea;
			if (safe == _applied && panelSize == _appliedPanelSize && screenSize == _appliedScreenSize)
			{
				return;
			}

			_applied = safe;
			_appliedPanelSize = panelSize;
			_appliedScreenSize = screenSize;

			Vector2 min = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMin, screenSize.y - safe.yMax));
			Vector2 max = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMax, screenSize.y - safe.yMin));
			foreach (VisualElement target in _targets)
			{
				target.style.position = Position.Absolute;
				target.style.left = Mathf.Max(0f, min.x);
				target.style.top = Mathf.Max(0f, min.y);
				target.style.right = Mathf.Max(0f, panelSize.x - max.x);
				target.style.bottom = Mathf.Max(0f, panelSize.y - max.y);
			}
		}
	}
}
