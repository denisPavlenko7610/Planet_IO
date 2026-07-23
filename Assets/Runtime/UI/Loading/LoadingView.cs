using UnityEngine;

namespace PlanetIO.UI.Loading
{
    public interface ILoadingView
    {
        void Render(float progress, string status);
    }

    public sealed class LoadingView : MonoBehaviour, ILoadingView
    {
        private GUIStyle _titleStyle;
        private GUIStyle _statusStyle;
        private float _progress;
        private string _status = string.Empty;

        public void Render(float progress, string status)
        {
            _progress = Mathf.Clamp01(progress);
            _status = status ?? string.Empty;
        }

        private void OnGUI()
        {
            EnsureStyles();

            float width = Mathf.Min(Screen.width * 0.72f, 720f);
            const float height = 28f;
            float left = (Screen.width - width) * 0.5f;
            float top = Screen.height * 0.55f;

            GUI.Label(
                new Rect(left, top - 72f, width, 36f),
                "Загрузка сессии",
                _titleStyle);

            GUI.Box(new Rect(left, top, width, height), GUIContent.none);
            GUI.Box(
                new Rect(
                    left + 3f,
                    top + 3f,
                    (width - 6f) * _progress,
                    height - 6f),
                GUIContent.none);

            GUI.Label(
                new Rect(left, top + 38f, width, 28f),
                $"{_status}  {_progress:P0}",
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
