using System.Collections;
using System.Linq;
using NUnit.Framework;
using RD.Haptics;
using RD.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlanetIO.Tests
{
	public sealed partial class GameFlowTests
	{
		[UnityTest]
		public IEnumerator EffectsVolume_MutesActiveBoostWithoutMutingMusic()
		{
			yield return OpenMenu();
			yield return WaitForMusicClip(new[] { "MenuTheme", "ExplorationTheme", "StellerDreams" }, 5f);
			yield return new WaitForSecondsRealtime(0.35f);

			ISettingsService settings = Resolve<ISettingsService>();
			float originalSfx = settings.Get(GameSettingKeys.SfxVolume);
			AudioSource music = GameObject.Find("Addressable Music").GetComponent<AudioSource>();
			float originalMusic = music.volume;
			GameObject target = new("Boost Audio Test");
			System.IDisposable loop = null;
			try
			{
				settings.Set(GameSettingKeys.SfxVolume, 0f);
				loop = Resolve<ISfxPlayer>().PlayLoop(SfxId.Boost, target.transform);
				yield return null;

				AudioSource boost = GameObject.Find("Sfx").GetComponentsInChildren<AudioSource>()
					.First(source => source.loop && source.isPlaying);
				Assert.That(boost.volume, Is.Zero);

				settings.Set(GameSettingKeys.SfxVolume, 0.8f);
				yield return null;

				Assert.That(boost.volume, Is.GreaterThan(0f));

				settings.Set(GameSettingKeys.SfxVolume, 0f);
				yield return null;

				Assert.That(boost.volume, Is.Zero);
				Assert.That(music.volume, Is.EqualTo(originalMusic).Within(0.001f));
				Assert.That(music.isPlaying, Is.True);
			}
			finally
			{
				loop?.Dispose();
				settings.Set(GameSettingKeys.SfxVolume, originalSfx);
				Object.Destroy(target);
			}
		}

		[UnityTest]
		[UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
		public IEnumerator HapticsSetting_ControlsTemplateModule()
		{
			yield return OpenMenu();

			ISettingsService settings = Resolve<ISettingsService>();
			bool original = settings.Get(GameSettingKeys.HapticsEnabled);
			int feedbackCount = 0;

			void OnFeedback() => feedbackCount++;

			Haptics.FallbackUsed += OnFeedback;
			try
			{
				settings.Set(GameSettingKeys.HapticsEnabled, false);
				Haptics.Play(HapticPreset.LightImpact);
				Assert.That(feedbackCount, Is.Zero);
				settings.Set(GameSettingKeys.HapticsEnabled, true);
				Haptics.Play(HapticPreset.Success);
				Assert.That(feedbackCount, Is.EqualTo(1));
			}
			finally
			{
				Haptics.FallbackUsed -= OnFeedback;
				settings.Set(GameSettingKeys.HapticsEnabled, original);
			}
		}
	}
}