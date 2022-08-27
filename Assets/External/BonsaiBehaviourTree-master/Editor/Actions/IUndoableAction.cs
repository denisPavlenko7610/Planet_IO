
namespace Bonsai.Designer
{
#if UNITY_EDITOR
  public interface IUndoableAction
  {
    void Undo();
    void Redo();
  }
  #endif
}
