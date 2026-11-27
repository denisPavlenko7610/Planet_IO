using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Hud
{
    public interface IDamageFeedbackView
    {
        void ShowDamage(Vector2 screenPosition, int score, float fraction, string source = "");
        void ClearDamage();
    }

    public sealed partial class SessionHudView : IDamageFeedbackView
    {
        private const float DamageFlashSeconds = 0.32f;
		private const float DamagePopupSeconds = 1.35f;
        private Label _damagePopup;
        private float _borderStrength;
        private float _damageFlash;
        private float _damageFlashRemaining;
        private float _damagePopupRemaining;
		private int _damageScore;
		private string _damageSource;
        private Vector2 _damageOrigin;

        public void ShowDamage(Vector2 screenPosition, int score, float fraction, string source = "")
        {
            if (_defeatVisible || _root?.panel == null || _damagePopup == null)
			{
				return;
			}
			bool coalesce = _damageSource == source && _damagePopupRemaining > DamagePopupSeconds - 0.12f;
			_damageScore = (coalesce ? _damageScore : 0) + Mathf.Max(0, score);
			_damageSource = source;
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(_root.panel,
                new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            _damageOrigin = _damagePopup.parent.WorldToLocal(panelPosition);
            float width = _damagePopup.parent.contentRect.width;
            float height = _damagePopup.parent.contentRect.height;
            _damageOrigin.x = Mathf.Clamp(_damageOrigin.x, Mathf.Min(170f, width * 0.5f), Mathf.Max(width * 0.5f, width - 170f));
            _damageOrigin.y = Mathf.Clamp(_damageOrigin.y - 50f, 110f, Mathf.Max(110f, height - 100f));
			string amount = _damageScore == 0 ? "−<1" : $"−{_damageScore:N0}";
			_damagePopup.text = string.IsNullOrEmpty(source) ? amount : $"{source}\n{amount}";
            _damagePopupRemaining = DamagePopupSeconds;
            _damageFlash = Mathf.Max(_damageFlash, Mathf.Lerp(0.35f, 0.85f, Mathf.Clamp01(fraction / 0.3f)));
            _damageFlashRemaining = DamageFlashSeconds;
            UpdateDamage(0f);
        }

        public void ClearDamage()
        {
            _damageFlashRemaining = _damagePopupRemaining = _damageFlash = 0f;
			_damageScore = 0;
			_damageSource = null;
            if (_damagePopup != null)
            {
                _damagePopup.style.opacity = 0f;
                _damagePopup.style.scale = new Scale(Vector3.one);
            }
            ApplyWarning();
        }

        private void UpdateDamage(float deltaTime)
        {
            _damageFlashRemaining = Mathf.Max(0f, _damageFlashRemaining - deltaTime);
            if (_damageFlashRemaining <= 0f)
			{
				_damageFlash = 0f;
			}
			ApplyWarning();
            if (_damagePopup == null || _damagePopupRemaining <= 0f)
			{
				return;
			}
			_damagePopupRemaining = Mathf.Max(0f, _damagePopupRemaining - deltaTime);
            float t = 1f - _damagePopupRemaining / DamagePopupSeconds;
            float rise = 1f - (1f - t) * (1f - t);
            float pop = Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI) * 0.22f;
            _damagePopup.style.left = _damageOrigin.x;
            _damagePopup.style.top = _damageOrigin.y - rise * 85f;
            _damagePopup.style.scale = new Scale(Vector3.one * (1f + pop));
            _damagePopup.style.opacity = Mathf.Clamp01((1f - t) / 0.55f);
        }

        private void ApplyWarning()
        {
            if (_borderWarning == null)
			{
				return;
			}
			float fade = Mathf.Clamp01(_damageFlashRemaining / DamageFlashSeconds);
            _borderWarning.style.opacity = Mathf.Max(_borderStrength, _damageFlash * fade * fade);
        }
    }
}
