namespace PlanetIO
{
    public static class LocalizationKeys
    {
        public const string TableName = "UI";

        public const string MenuNicknamePlaceholder = "menu.nickname_placeholder";
        public const string MenuRandomNickname = "menu.random_nickname";
        public const string MenuPlayWithBots = "menu.play_with_bots";
        public const string MenuCreateRoom = "menu.create_room";
        public const string MenuJoinRoom = "menu.join_room";
        public const string MenuRoomCodePlaceholder = "menu.room_code_placeholder";
        public const string MenuSettings = "menu.settings";
        public const string MenuBestScore = "menu.best_score";
        public const string MenuOnline = "menu.online";
        public const string MenuColor = "menu.color";

        public const string StatusReady = "status.ready";
        public const string StatusCreatingRoom = "status.creating_room";
        public const string StatusRoomCreated = "status.room_created";
        public const string StatusConnecting = "status.connecting";
        public const string StatusStartingSolo = "status.starting_solo";
        public const string StatusLoading = "status.loading";
        public const string StatusLeaving = "status.leaving";
        public const string StatusFailed = "status.failed";
        public const string StatusEnterRoomCode = "status.enter_room_code";
        public const string StatusHostLeft = "status.host_left";
        public const string StatusRoomFull = "status.room_full";
        public const string StatusVersionMismatch = "status.version_mismatch";

        public const string SettingsTitle = "settings.title";
        public const string SettingsMusic = "settings.music";
        public const string SettingsSound = "settings.sound";
        public const string SettingsLanguage = "settings.language";
        public const string SettingsHaptics = "settings.haptics";
        public const string SettingsAdPrivacy = "settings.ad_privacy";
        public const string SettingsClose = "settings.close";

        public const string HudSinglePlayer = "hud.single_player";
        public const string HudRoom = "hud.room";
        public const string HudPlayers = "hud.players";
        public const string HudRank = "hud.rank";
        public const string HudLeaders = "hud.leaders";
        public const string HudHintTouch = "hud.hint_touch";
        public const string HudHintDesktop = "hud.hint_desktop";
        public const string HudYouAte = "hud.you_ate";
        public const string HudYouLost = "hud.you_lost";
        public const string HudFinalScore = "hud.final_score";
        public const string HudPlayAgain = "hud.play_again";
        public const string HudWatchAd = "hud.watch_ad";
        public const string HudLeave = "hud.leave";
        public const string HudBoost = "hud.boost";
        public const string HudBackToLeave = "hud.back_to_leave";

        public static readonly string[] All =
        {
            MenuNicknamePlaceholder, MenuRandomNickname, MenuPlayWithBots, MenuCreateRoom, MenuJoinRoom,
            MenuRoomCodePlaceholder, MenuSettings, MenuBestScore, MenuOnline, MenuColor,
            StatusReady, StatusCreatingRoom, StatusRoomCreated, StatusConnecting, StatusStartingSolo,
            StatusLoading, StatusLeaving, StatusFailed, StatusEnterRoomCode, StatusHostLeft, StatusRoomFull, StatusVersionMismatch,
            SettingsTitle, SettingsMusic, SettingsSound, SettingsLanguage, SettingsHaptics, SettingsAdPrivacy, SettingsClose,
            HudSinglePlayer, HudRoom, HudPlayers, HudRank, HudLeaders, HudHintTouch, HudHintDesktop,
            HudYouAte, HudYouLost, HudFinalScore, HudPlayAgain, HudWatchAd, HudLeave, HudBoost, HudBackToLeave
        };
    }
}
