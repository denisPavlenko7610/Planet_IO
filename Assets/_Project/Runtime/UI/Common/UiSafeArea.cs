using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO.UI
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiSafeArea : MonoBehaviour
    {
        [SerializeField] private string _elementName = "safe-area";

        private UIDocument _document;
        private VisualElement _target;
        private Rect _applied;
        private Vector2 _appliedPanelSize;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            _target = _document.rootVisualElement.Q<VisualElement>(_elementName);
            UiPointerBlocker.Register(_document);
            _applied = default;
            Apply();
        }

        private void OnDisable()
        {
            UiPointerBlocker.Unregister(_document);
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            IPanel panel = _target?.panel;
            if (panel == null)
            {
                return;
            }

            Vector2 panelSize = _document.rootVisualElement.layout.size;
            Rect safe = Screen.safeArea;
            if (panelSize.x <= 0f || (safe == _applied && panelSize == _appliedPanelSize))
            {
                return;
            }

            _applied = safe;
            _appliedPanelSize = panelSize;

            Vector2 min = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMin, Screen.height - safe.yMax));
            Vector2 max = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMax, Screen.height - safe.yMin));

            _target.style.position = Position.Absolute;
            _target.style.left = Mathf.Max(0f, min.x);
            _target.style.top = Mathf.Max(0f, min.y);
            _target.style.right = Mathf.Max(0f, panelSize.x - max.x);
            _target.style.bottom = Mathf.Max(0f, panelSize.y - max.y);
        }
    }
}
