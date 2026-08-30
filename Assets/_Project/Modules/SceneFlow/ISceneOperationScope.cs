using System;

namespace UnityTemplates.SceneFlow
{
	public interface ISceneOperationScope
	{
		IDisposable Enter();
	}
}
