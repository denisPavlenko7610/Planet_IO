using Planet_IO;
using UnityEngine;
using VContainer;

namespace PlanetIO.UI.Loading
{
    public sealed class LoadingView : MonoBehaviour
    {
        private INetworkSessionService _session;
        private GUIStyle _titleStyle;
        private GUIStyle _statusStyle;

        [Inject]
        public void Construct(INetworkSessionService session)
        {
            _session = session;
        }

        private void OnGUI()
        {
            if (_session == null)
            {
                return;
            }

            EnsureStyles();

            float width = Mathf.Min(Screen.width * 0.72f, 720f);
            float height = 28f;
            float left = (Screen.width - width) * 0.5f;
            float top = Screen.height * 0.55f;
            float progress = Mathf.Clamp01(_session.LoadingProgress);

            GUI.Label(
                new Rect(left, top - 72f, width, 36f),
                "Загрузка мультиплеерной сессии",
                _titleStyle);

            GUI.Box(new Rect(left, top, width, height), GUIContent.none);
            GUI.Box(
                new Rect(left + 3f, top + 3f, (width - 6f) * progress, height - 6f),
                GUIContent.none);

            GUI.Label(
                new Rect(left, top + 38f, width, 28f),
                $"{_session.Status}  {progress:P0}",
                _statusStyle);
        }

        private void EnsureStyles()
        {
            _titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                fontStyle = FontStyle.Bold
            };

            _statusStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17
            };
        }
    }
}
