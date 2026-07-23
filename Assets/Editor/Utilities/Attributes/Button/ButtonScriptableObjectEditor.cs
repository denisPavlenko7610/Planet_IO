using UnityEditor;
using UnityEngine;

namespace TLN.Editor.Validation
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(ScriptableObject), true)]
	public sealed class ButtonScriptableObjectEditor : ButtonEditorBase
	{
	}
}
