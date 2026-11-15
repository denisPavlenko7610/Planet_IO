using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public sealed partial class Player
    {
        private void OnSkinChanged(byte _, byte skin)
        {
            ApplySkin(skin);
        }

        private void ApplySkin(byte skin)
        {
            if (!_skinCatalog.IsValid(skin) || !TryGetComponent(out SpriteRenderer body))
            {
                return;
            }

            Sprite sprite = _skinCatalog.Get(skin).Sprite;
            if (sprite != null)
            {
                body.sprite = sprite;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetSkinRpc(byte skin)
        {
            if (_skinCatalog.IsValid(skin))
            {
                _networkSkin.Value = skin;
            }
        }

        private void OnColorChanged(Color32 _, Color32 color)
        {
            _visualEffects?.SetBaseColor(color);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetColorRpc(Color32 color)
        {
            _networkColor.Value = color;
        }

        private void OnSpawnProtectionChanged(bool _, bool protection)
        {
            _visualEffects?.SetSpawnProtected(protection);
        }

        private void ApplyVisualState()
        {
            if (_visualEffects == null)
            {
                return;
            }

            _visualEffects.SetBaseColor(_networkColor.Value);
            ApplySkin(_networkSkin.Value);
            _visualEffects.SetBoosting(_networkBoosting.Value);
            _visualEffects.SetSpawnProtected(_networkSpawnProtected.Value);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitNicknameRpc(FixedString64Bytes nickname)
        {
            SetDisplayName(NicknameRules.Normalize(nickname.ToString()));
        }
    }
}
