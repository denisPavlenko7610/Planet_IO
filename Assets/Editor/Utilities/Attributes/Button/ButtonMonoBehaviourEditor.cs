using UnityEditor;
using UnityEngine;

namespace TLN.Editor.Validation
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(MonoBehaviour), true)]
	public sealed class ButtonMonoBehaviourEditor : ButtonEditorBase
	{
	}
}
