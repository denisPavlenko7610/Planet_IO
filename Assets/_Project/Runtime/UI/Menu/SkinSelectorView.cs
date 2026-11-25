using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Menu
{
	public interface ISkinSelectorView
	{
		event Action<int> SkinClicked;
		event Action<int> RewardedUnlockRequested;

		void Build(IReadOnlyList<Sprite> sprites, int selectedIndex);
		void SetLabels(IReadOnlyList<string> names, string select, string equipped, string price, string rewarded);
		void SetState(int index, bool unlocked, bool selected);
		void SetActions(bool canBuy, bool busy);
		void ShowAdStatus(string message, bool isError);
	}

	[DefaultExecutionOrder(100)]
	[RequireComponent(typeof(UIDocument))]
	public sealed class SkinSelectorView : MonoBehaviour, ISkinSelectorView
	{
		private IReadOnlyList<Sprite> _sprites;
		private IReadOnlyList<string> _names;
		private bool[] _unlocked;
		private int _previewIndex;
		private int _selectedIndex = -1;
		private VisualElement _profileSkin;
		private VisualElement _preview;
		private Label _name;
		private Label _status;
		private Button _previous;
		private Button _next;
		private Button _select;
		private Button _rewarded;
		private string _selectLabel;
		private string _equippedLabel;
		private string _priceLabel;
		private bool _canBuy;
		private bool _busy;

		public event Action<int> SkinClicked;
		public event Action<int> RewardedUnlockRequested;

		public void Build(IReadOnlyList<Sprite> sprites, int selectedIndex)
		{
			VisualElement root = GetComponent<UIDocument>().rootVisualElement;
			_profileSkin = root.Q("profile-skin");
			_preview = root.Q("skin-preview");
			_name = root.Q<Label>("skin-name");
			_status = root.Q<Label>("customize-status");
			_previous = root.Q<Button>("skin-previous");
			_next = root.Q<Button>("skin-next");
			_select = root.Q<Button>("skin-select");
			_rewarded = root.Q<Button>("skin-rewarded");
			_sprites = sprites;
			_unlocked = new bool[sprites.Count];
			_previewIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, sprites.Count - 1));

			if (sprites.Count == 0)
			{
				root.Q("skins").style.display = DisplayStyle.None;
			}

			_previous.clicked += OnPrevious;
			_next.clicked += OnNext;
			_select.clicked += OnSelect;
			_rewarded.clicked += OnRewarded;
		}

		private void OnDestroy()
		{
			if (_previous == null)
			{
				return;
			}

			_previous.clicked -= OnPrevious;
			_next.clicked -= OnNext;
			_select.clicked -= OnSelect;
			_rewarded.clicked -= OnRewarded;
		}

		public void SetLabels(IReadOnlyList<string> names, string select, string equipped, string price, string rewarded)
		{
			_names = names;
			_selectLabel = select;
			_equippedLabel = equipped;
			_priceLabel = price;
			_rewarded.text = rewarded;
			Render();
		}

		public void SetState(int index, bool unlocked, bool selected)
		{
			_unlocked[index] = unlocked;

			if (selected)
			{
				_selectedIndex = index;
				_profileSkin.style.backgroundImage = new StyleBackground(_sprites[index]);
			}

			Render();
		}

		public void SetActions(bool canBuy, bool busy)
		{
			_canBuy = canBuy;
			_busy = busy;
			Render();
		}

		public void ShowAdStatus(string message, bool isError)
		{
			_status.text = message;
			_status.EnableInClassList("status--error", isError);
		}

		private void OnPrevious() => Browse(-1);

		private void OnNext() => Browse(1);

		private void OnSelect() => SkinClicked?.Invoke(_previewIndex);

		private void OnRewarded() => RewardedUnlockRequested?.Invoke(_previewIndex);

		private void Browse(int direction)
		{
			_previewIndex = (_previewIndex + direction + _sprites.Count) % _sprites.Count;
			ShowAdStatus(string.Empty, false);
			Render();
		}

		private void Render()
		{
			if (_sprites.Count == 0 || _names == null)
			{
				return;
			}

			bool unlocked = _unlocked[_previewIndex];
			bool equipped = unlocked && _previewIndex == _selectedIndex;
			_preview.style.backgroundImage = new StyleBackground(_sprites[_previewIndex]);
			_name.text = _names[_previewIndex];
			_select.text = equipped ? _equippedLabel : unlocked ? _selectLabel : _priceLabel;
			_select.SetEnabled(!_busy && !equipped && (unlocked || _canBuy));
			_rewarded.style.display = unlocked ? DisplayStyle.None : DisplayStyle.Flex;
			_rewarded.SetEnabled(!_busy);
			_previous.SetEnabled(!_busy && _sprites.Count > 1);
			_next.SetEnabled(!_busy && _sprites.Count > 1);
		}
	}
}
