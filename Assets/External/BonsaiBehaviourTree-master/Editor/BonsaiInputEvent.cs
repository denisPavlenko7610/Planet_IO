
using UnityEngine;

namespace Bonsai.Designer
{
#if UNITY_EDITOR
  public class BonsaiInputEvent
  {
    public CanvasTransform transform;
    public Vector2 canvasMousePostion;
    public BonsaiNode node;
    public bool isInputFocused;
    public bool isOutputFocused;

    public bool IsPortFocused()
    {
      return isInputFocused || isOutputFocused;
    }

    public bool IsNodeFocused()
    {
      return node != null;
    }
  }
  #endif
}
