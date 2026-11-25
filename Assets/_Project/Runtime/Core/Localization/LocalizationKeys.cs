namespace PlanetIO
{
    public static class LocalizationKeys
    {
        public const string TableName = "UI";

        public const string MenuNicknamePlaceholder = "menu.nickname_placeholder";
        public const string MenuRandomNickname = "menu.random_nickname";
        public const string MenuPlayWithBots = "menu.play_with_bots";
        public const string MenuCreateRoom = "menu.create_room";
        public const string MenuQuickPlay = "menu.quick_play";
        public const string MenuJoinRoom = "menu.join_room";
        public const string MenuRoomCodePlaceholder = "menu.room_code_placeholder";
        public const string MenuSettings = "menu.settings";
        public const string MenuBestScore = "menu.best_score";
        public const string MenuOnline = "menu.online";
        public const string MenuPlanet = "menu.planet";
        public const string MenuSkinLocked = "menu.skin_locked";
        public const string MenuAdUnavailable = "menu.ad_unavailable";

        public const string StatusReady = "status.ready";
        public const string StatusCreatingRoom = "status.creating_room";
        public const string StatusRoomCreated = "status.room_created";
        public const string StatusConnecting = "status.connecting";
        public const string StatusStartingSolo = "status.starting_solo";
        public const string StatusLoading = "status.loading";
        public const string StatusLeaving = "status.leaving";
        public const string StatusFailed = "status.failed";
        public const string StatusEnterRoomCode = "status.enter_room_code";
        public const string StatusMigrating = "status.migrating";
        public const string StatusHostLeft = "status.host_left";
        public const string StatusSearching = "status.searching";
        public const string StatusRoomFull = "status.room_full";
        public const string StatusVersionMismatch = "status.version_mismatch";

        public const string SettingsTitle = "settings.title";
        public const string SettingsMusic = "settings.music";
        public const string SettingsSound = "settings.sound";
        public const string SettingsBotDifficulty = "settings.bot_difficulty";
        public const string SettingsBotsOnly = "settings.bots_only";
        public const string DifficultyEasy = "difficulty.easy";
        public const string DifficultyMedium = "difficulty.medium";
        public const string DifficultyHard = "difficulty.hard";
        public const string SettingsLanguage = "settings.language";
        public const string SettingsHaptics = "settings.haptics";
        public const string SettingsControlsSwapped = "settings.controls_swapped";
        public const string SettingsFixedJoystick = "settings.fixed_joystick";
        public const string SettingsAdPrivacy = "settings.ad_privacy";
        public const string SettingsPrivacyPolicy = "settings.privacy_policy";
        public const string SettingsClose = "settings.close";
        public const string SettingsPlayGamesSignIn = "settings.play_games_sign_in";
        public const string SettingsLeaderboards = "settings.leaderboards";
        public const string SettingsAchievements = "settings.achievements";

        public const string HudSinglePlayer = "hud.single_player";
        public const string HudRoom = "hud.room";
        public const string HudPlayers = "hud.players";
        public const string HudRank = "hud.rank";
        public const string HudLeaders = "hud.leaders";
        public const string HudHintTouch = "hud.hint_touch";
        public const string HudYouAte = "hud.you_ate";
        public const string HudYouLost = "hud.you_lost";
        public const string HudFinalScore = "hud.final_score";
        public const string HudPlayAgain = "hud.play_again";
        public const string HudWatchAd = "hud.watch_ad";
        public const string HudLeave = "hud.leave";
        public const string HudBoost = "hud.boost";
        public const string HudBackToLeave = "hud.back_to_leave";
        public const string HudKillFeed = "hud.kill_feed";
        public const string TutorialEat = "tutorial.eat";
        public const string TutorialAvoid = "tutorial.avoid";
        public const string TutorialBoost = "tutorial.boost";

        public const string ResultDetails = "result.details";
        public const string ResultKilledBy = "result.killed_by";
        public const string ResultRank = "result.rank";
        public const string ResultReward = "result.reward";
        public const string ResultRankUp = "result.rank_up";
        public const string ResultDoubleDust = "result.double_dust";
        public const string ProgressRankDust = "progress.rank_dust";
        public const string MissionsButton = "missions.button";
        public const string MissionsTitle = "missions.title";
        public const string MissionsStreak = "missions.streak";
        public const string MissionsStreakBonus = "missions.streak_bonus";
        public const string MissionsClaim = "missions.claim";
        public const string MissionsReroll = "missions.reroll";
        public const string MissionsDone = "missions.done";
        public const string MissionKills = "mission.kills";
        public const string MissionPeakTier = "mission.peak_tier";
        public const string MissionSurvive = "mission.survive";
        public const string MissionPeakScore = "mission.peak_score";
        public const string MissionPerks = "mission.perks";
        public const string MissionRuns = "mission.runs";
        public const string MenuSkinLockedPrice = "menu.skin_locked_price";
        public const string FtueMove = "ftue.move";
        public const string FtueAvoid = "ftue.avoid";
        public const string FtueHunt = "ftue.hunt";
        public const string FtueTier = "ftue.tier";

        public static string MissionName(int definition) => MissionRules.Metric(definition) switch
        {
            MissionMetric.Kills => MissionKills,
            MissionMetric.PeakTier => MissionPeakTier,
            MissionMetric.SurviveSeconds => MissionSurvive,
            MissionMetric.PeakScore => MissionPeakScore,
            MissionMetric.Perks => MissionPerks,
            _ => MissionRuns
        };

        public const string EventMeteorWarn = "event.meteor.warn";
        public const string EventMeteorNow = "event.meteor.now";
        public const string EventSupernovaWarn = "event.supernova.warn";
        public const string EventSupernovaNow = "event.supernova.now";
        public const string EventBlackHoleWarn = "event.blackhole.warn";
        public const string EventBlackHoleNow = "event.blackhole.now";
        public const string ModifierLowGravity = "modifier.low_gravity";
        public const string ModifierHeavyGravity = "modifier.heavy_gravity";
        public const string ModifierGoldRush = "modifier.gold_rush";

        public static string EventWarning(CosmicEventType type) => type switch
        {
            CosmicEventType.MeteorShower => EventMeteorWarn,
            CosmicEventType.Supernova => EventSupernovaWarn,
            _ => EventBlackHoleWarn
        };

        public static string EventStarted(CosmicEventType type) => type switch
        {
            CosmicEventType.MeteorShower => EventMeteorNow,
            CosmicEventType.Supernova => EventSupernovaNow,
            _ => EventBlackHoleNow
        };

        public static string ModifierName(DailyModifier modifier) => modifier switch
        {
            DailyModifier.LowGravity => ModifierLowGravity,
            DailyModifier.HeavyGravity => ModifierHeavyGravity,
            DailyModifier.GoldRush => ModifierGoldRush,
            _ => null
        };

        public const string PerkTitle = "perk.title";
        public const string PerkIonTrailName = "perk.ion_trail.name";
        public const string PerkIonTrailDesc = "perk.ion_trail.desc";
        public const string PerkMagnetosphereName = "perk.magnetosphere.name";
        public const string PerkMagnetosphereDesc = "perk.magnetosphere.desc";
        public const string PerkHeavyMoonsName = "perk.heavy_moons.name";
        public const string PerkHeavyMoonsDesc = "perk.heavy_moons.desc";
        public const string PerkAblativeCrustName = "perk.ablative_crust.name";
        public const string PerkAblativeCrustDesc = "perk.ablative_crust.desc";
        public const string PerkSolarSailName = "perk.solar_sail.name";
        public const string PerkSolarSailDesc = "perk.solar_sail.desc";
        public const string PerkGravityWellName = "perk.gravity_well.name";
        public const string PerkGravityWellDesc = "perk.gravity_well.desc";
        public const string PerkNebulaVeilName = "perk.nebula_veil.name";
        public const string PerkNebulaVeilDesc = "perk.nebula_veil.desc";
        public const string PerkCometRiderName = "perk.comet_rider.name";
        public const string PerkCometRiderDesc = "perk.comet_rider.desc";
        public const string PerkPredatorName = "perk.predator.name";
        public const string PerkPredatorDesc = "perk.predator.desc";

        public static string PerkName(PerkId id) => id switch
        {
            PerkId.IonTrail => PerkIonTrailName,
            PerkId.Magnetosphere => PerkMagnetosphereName,
            PerkId.HeavyMoons => PerkHeavyMoonsName,
            PerkId.AblativeCrust => PerkAblativeCrustName,
            PerkId.SolarSail => PerkSolarSailName,
            PerkId.GravityWell => PerkGravityWellName,
            PerkId.NebulaVeil => PerkNebulaVeilName,
            PerkId.CometRider => PerkCometRiderName,
            _ => PerkPredatorName
        };

        public static string PerkDescription(PerkId id) => id switch
        {
            PerkId.IonTrail => PerkIonTrailDesc,
            PerkId.Magnetosphere => PerkMagnetosphereDesc,
            PerkId.HeavyMoons => PerkHeavyMoonsDesc,
            PerkId.AblativeCrust => PerkAblativeCrustDesc,
            PerkId.SolarSail => PerkSolarSailDesc,
            PerkId.GravityWell => PerkGravityWellDesc,
            PerkId.NebulaVeil => PerkNebulaVeilDesc,
            PerkId.CometRider => PerkCometRiderDesc,
            _ => PerkPredatorDesc
        };

		public const string MenuSkinSelect = "menu.skin_select";
		public const string MenuSkinEquipped = "menu.skin_equipped";
		public const string MenuSkinBuy = "menu.skin_buy";
		public const string MenuSkinWatch = "menu.skin_watch";
		public const string MissionsReset = "missions.reset";
		public const string MissionProgressReward = "missions.progress_reward";
		public const string PerkAutoPick = "perk.auto_pick";
		public const string DamageComet = "damage.comet";
		public const string DamageMoon = "damage.moon";
		public const string DamageBorder = "damage.border";
		public const string DamageIonTrail = "damage.ion_trail";
		public const string DamageBlackHole = "damage.black_hole";
		public const string TutorialContinue = "tutorial.continue";
		public const string TutorialSkip = "tutorial.skip";
		public const string TutorialBot = "tutorial.bot";
		public const string TutorialComet = "tutorial.comet";
		public const string TutorialBlackHole = "tutorial.black_hole";
		public const string TutorialEvolution = "tutorial.evolution";
		public const string TutorialRank = "tutorial.rank";

		public static string SkinName(string id) => "skin." + id;

        public static readonly string[] All =
        {
            MenuNicknamePlaceholder, MenuRandomNickname, MenuPlayWithBots, MenuCreateRoom, MenuQuickPlay, MenuJoinRoom,
            MenuRoomCodePlaceholder, MenuSettings, MenuBestScore, MenuOnline, MenuPlanet, MenuSkinLocked, MenuAdUnavailable,
            StatusReady, StatusCreatingRoom, StatusRoomCreated, StatusConnecting, StatusStartingSolo,
            StatusLoading, StatusLeaving, StatusFailed, StatusEnterRoomCode, StatusMigrating, StatusHostLeft, StatusSearching, StatusRoomFull, StatusVersionMismatch,
            SettingsBotDifficulty, SettingsBotsOnly, DifficultyEasy, DifficultyMedium, DifficultyHard,
            SettingsTitle, SettingsMusic, SettingsSound, SettingsLanguage, SettingsHaptics, SettingsControlsSwapped, SettingsFixedJoystick, SettingsAdPrivacy, SettingsPrivacyPolicy, SettingsClose,
            SettingsPlayGamesSignIn, SettingsLeaderboards, SettingsAchievements,
            HudSinglePlayer, HudRoom, HudPlayers, HudRank, HudLeaders, HudHintTouch,
            HudYouAte, HudYouLost, HudFinalScore, HudPlayAgain, HudWatchAd, HudLeave, HudBoost, HudBackToLeave,
            HudKillFeed, TutorialEat, TutorialAvoid, TutorialBoost,
            ResultDetails, ResultKilledBy, ResultRank, ResultReward, ResultRankUp, ResultDoubleDust, ProgressRankDust,
            MissionsButton, MissionsTitle, MissionsStreak, MissionsStreakBonus, MissionsClaim, MissionsReroll, MissionsDone,
            MissionKills, MissionPeakTier, MissionSurvive, MissionPeakScore, MissionPerks, MissionRuns,
            MenuSkinLockedPrice, FtueMove, FtueAvoid, FtueHunt, FtueTier,
			MenuSkinSelect, MenuSkinEquipped, MenuSkinBuy, MenuSkinWatch,
			MissionsReset, MissionProgressReward, PerkAutoPick, DamageComet,
			DamageMoon, DamageBorder, DamageIonTrail, DamageBlackHole,
			TutorialContinue, TutorialSkip, TutorialBot, TutorialComet, TutorialBlackHole,
			TutorialEvolution, TutorialRank, SkinName("green"), SkinName("violet"),
			SkinName("sun"), SkinName("candy"), SkinName("heart"), SkinName("crystal"),
			SkinName("swirl"), SkinName("pizza"), SkinName("burger"), SkinName("cheese"),
			SkinName("cake"), SkinName("lava"),
            EventMeteorWarn, EventMeteorNow, EventSupernovaWarn, EventSupernovaNow, EventBlackHoleWarn, EventBlackHoleNow,
			ModifierLowGravity, ModifierHeavyGravity, ModifierGoldRush,
            PerkTitle,
            PerkIonTrailName, PerkIonTrailDesc, PerkMagnetosphereName, PerkMagnetosphereDesc,
            PerkHeavyMoonsName, PerkHeavyMoonsDesc, PerkAblativeCrustName, PerkAblativeCrustDesc,
            PerkSolarSailName, PerkSolarSailDesc, PerkGravityWellName, PerkGravityWellDesc,
            PerkNebulaVeilName, PerkNebulaVeilDesc, PerkCometRiderName, PerkCometRiderDesc,
            PerkPredatorName, PerkPredatorDesc
        };
    }
}
