using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Johans.RelayTest
{
    public class RelayTestPlayer : NetworkBehaviour
    {
        private readonly NetworkVariable<FixedString128Bytes> displayName = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            displayName.Value = new FixedString128Bytes(RelayTestMenu.LocalPlayerName);
            float angle = (OwnerClientId % 4) * Mathf.PI * .5f;
            transform.position = new Vector3(Mathf.Cos(angle) * 3, 1, Mathf.Sin(angle) * 3);
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || Keyboard.current == null || !Application.isFocused) return;
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (selected && selected.TryGetComponent<InputField>(out var input) && input.isFocused) return;
            var keys = Keyboard.current;
            var move = new Vector3(
                (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0),
                0,
                (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0));
            var next = transform.position + Vector3.ClampMagnitude(move, 1) * (5 * Time.deltaTime);
            next.x = Mathf.Clamp(next.x, -8, 8);
            next.z = Mathf.Clamp(next.z, -6, 6);
            transform.position = next;
        }

        private void OnGUI()
        {
            if (!IsSpawned || !Camera.main) return;
            var point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
            if (point.z <= 0) return;
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, richText = false };
            GUI.Box(new Rect(point.x - 90, Screen.height - point.y, 180, 25),
                displayName.Value.ToString() + (IsOwner ? " (du)" : ""), style);
        }
    }
}
