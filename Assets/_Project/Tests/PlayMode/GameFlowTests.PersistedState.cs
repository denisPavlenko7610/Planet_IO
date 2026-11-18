using System.Collections;
using NUnit.Framework;
using RD.Settings;
using UnityEngine.TestTools;

namespace PlanetIO.Tests
{
	public sealed partial class GameFlowTests
	{
		[UnityOneTimeSetUp]
		public IEnumerator ResetPersistedGameplaySettings()
		{
			yield return OpenMenu();
			ISettingsService settings = Resolve<ISettingsService>();
			settings.Reset(GameSettingKeys.BotDifficulty);
			settings.Reset(GameSettingKeys.BlitzMode);
			settings.Flush();
		}
	}
}
