using System.Collections;
using System.IO;
using System.Linq;
using EmberThrone.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmberThrone.Game.Tests
{
    [Explicit("Takes screenshots; run on demand.")]
    public class ScreenshotTour
    {
        const string Folder = "TestResults/screens";
        const string Even = "YPOGY";
        const string Odd = "OGYPO";
        const float SlowMotion = 0.1f;

        GameApp _app;
        RenderTexture _target;
        RenderTexture _uiTarget;
        System.Collections.Generic.Dictionary<int, int> _savedStars;
        string _savedProfile;
        GameController Game => _app.Game;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Directory.CreateDirectory(Folder);
            _savedStars = StarsBackup.Snapshot();
            StarsBackup.Restore(new System.Collections.Generic.Dictionary<int, int>());
            _savedProfile = StarsBackup.SnapshotProfile();
            StarsBackup.FreshProfile();
            Meta.Economy.AddCoins(1500);
            Meta.Economy.LoseLife(Meta.Now);
            var quests = Meta.Economy.Quests(Meta.Now);
            Meta.Economy.Report(quests[0].Kind, quests[0].Target, Meta.Now);
            Meta.Economy.Report(quests[1].Kind, quests[1].Target / 2, Meta.Now);
            Meta.Save();
            Time.timeScale = 1f;
            GameApp.ShowLoading = false;
            GameApp.ShowStories = false;
            GameApp.StreakBoosts = false;
            GameApp.OfferStarter = false;
            UiRoot.SafeAreaOverride = null;
            UiRoot.ScreenSizeOverride = new Vector2Int(1080, 1920);
            _app = GameApp.Create();
            UseScreen(1080, 1920, null);
            for (int i = 0; i < 3; i++) yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            GameApp.ShowLoading = true;
            GameApp.ShowStories = true;
            GameApp.StreakBoosts = true;
            GameApp.OfferStarter = true;
            UiRoot.SafeAreaOverride = null;
            UiRoot.ScreenSizeOverride = null;
            Object.Destroy(_app.gameObject);
            Object.Destroy(_target);
            Object.Destroy(_uiTarget);
            StarsBackup.Restore(_savedStars);
            StarsBackup.RestoreProfile(_savedProfile);
            yield return null;
        }

        void UseScreen(int width, int height, Rect? safeArea)
        {
            UiRoot.ScreenSizeOverride = new Vector2Int(width, height);
            UiRoot.SafeAreaOverride = safeArea;
            var oldBoard = _target;
            var oldUi = _uiTarget;
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _app.Camera.targetTexture = _target;

            _uiTarget = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var settings = _app.Ui.Settings;
            settings.targetTexture = _uiTarget;
            settings.clearColor = true;
            settings.colorClearValue = Color.clear;

            if (oldBoard != null) Object.Destroy(oldBoard);
            if (oldUi != null) Object.Destroy(oldUi);
        }

        static readonly float[] Linear = BuildLinear();

        static float[] BuildLinear()
        {
            var table = new float[256];
            for (int i = 0; i < 256; i++) table[i] = Mathf.GammaToLinearSpace(i / 255f);
            return table;
        }

        static byte Over(byte ui, byte board, float k)
        {
            float linear = Mathf.Min(1f, Linear[ui] + Linear[board] * k);
            return (byte)Mathf.RoundToInt(Mathf.LinearToGammaSpace(linear) * 255f);
        }

        static Color32[] Read(RenderTexture rt)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            var pixels = tex.GetPixels32();
            Object.Destroy(tex);
            return pixels;
        }

        IEnumerator Shot(string name)
        {
            yield return null;
            _app.Camera.Render();
            var board = Read(_target);
            var ui = Read(_uiTarget);
            for (int i = 0; i < board.Length; i++)
            {
                var u = ui[i];
                var b = board[i];
                float k = 1f - u.a / 255f;
                board[i] = new Color32(Over(u.r, b.r, k), Over(u.g, b.g, k), Over(u.b, b.b, k), 255);
            }
            var tex = new Texture2D(_target.width, _target.height, TextureFormat.RGBA32, false);
            tex.SetPixels32(board);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        static LevelDefinition Level(int moves, GoalDefinition goal, params string[] board)
        {
            var level = LevelParser.FromLayers(board, null, null, null);
            level.Name = "Deneme";
            level.Id = 99;
            level.Moves = moves;
            level.ColorCount = 5;
            level.Goals.Add(goal ?? new GoalDefinition(GoalType.Color, 40, GemColor.Red));
            return level;
        }

        IEnumerator Begin(LevelDefinition level, int seed = 7)
        {
            _app.StartLevel(level, 0, seed);
            _app.Game.HintDelay = float.MaxValue;
            for (int i = 0; i < 3; i++) yield return null;
            yield return GameTime(_app.Game.View.IntroLength + 0.05f);
        }

        IEnumerator WaitIdle()
        {
            yield return null;
            while (Game.IsBusy) yield return null;
        }

        static IEnumerator GameTime(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator TakeAll()
        {
            _app.ShowLoadingScreen();
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("00_yukleme");
            _app.Loading?.Close();
            for (int chapter = 0; chapter < LevelMap.Chapters.Length; chapter++)
            {
                if (!StoryScene.Available(chapter)) continue;
                _app.PlayStory(chapter);
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Shot("00" + (char)('a' + chapter) + "_sahne_" + (chapter + 1) + "_basi");
                yield return new WaitForSecondsRealtime(6.5f);
                yield return Shot("00" + (char)('a' + chapter) + "_sahne_" + (chapter + 1) + "_sonu");
                _app.Story?.Close(false);
            }
            _app.Loading?.Close();
            yield return Shot("01_seviye_secimi");
            _app.LevelSelect.ShowMap(true);
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01a_dunya");
            _app.LevelSelect.Scroll.scrollOffset = new Vector2(0f, 1e6f);
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01a_dunya_sonu");
            _app.LevelSelect.ShowMap(false);
            yield return null;
            for (int c = 0; c < 4; c++) Meta.Economy.Profile.Cards[Meta.CardRules.IndexOf(0, c)] = 1;
            for (int c = 0; c < Meta.CardRules.Sets[2].Cards.Length; c++) Meta.Economy.Profile.Cards[Meta.CardRules.IndexOf(2, c)] = c == 0 ? 2 : 1;
            Meta.Save();
            _app.LevelSelect.Show(LobbyPage.Cards);
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01h_koleksiyon");
            _app.OpenCardSet(2);
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01i_kart_seti");
            _app.Modal.Close();
            _app.OpenCardSet(0);
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01i_kart_seti_eksik");
            _app.Modal.Close();
            _app.OpenPack();
            yield return GameTime(Timing.ModalIn + Timing.StarFill * 4f);
            yield return Shot("01j_kart_paketi");
            _app.Modal.Close();

            long real = Meta.WallClock();
            long shown = RaceRules.WeekStart(RaceRules.WeekOf(real)) + 3 * EconomyRules.SecondsPerDay + 14 * 3600;
            Meta.Clock = () => shown;
            for (int i = 0; i < 9; i++) Meta.Race.AddWin(3, true, shown);
            Meta.Save();
            _app.LevelSelect.Show(LobbyPage.Rank);
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01k_siralama");
            _app.LevelSelect.Show(LobbyPage.Team);
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01l_takimlar");
            Meta.Team.Join(2);
            for (int guard = 0; Meta.Team.OpenAsks(shown) == 0 && guard < 600; guard++) shown += 300;
            Meta.Economy.LoseLife(shown);
            Meta.Economy.LoseLife(shown);
            Meta.Team.Request(shown);
            shown += 200;
            Meta.Save();
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01m_takim");
            Meta.Team.Leave();

            long harvest = (long)(new System.DateTime(2026, 11, 12, 14, 0, 0) - new System.DateTime(1970, 1, 1)).TotalSeconds;
            Meta.Clock = () => harvest;
            Meta.Event.Report(EventMetric.Obstacles, 380, harvest);
            Meta.Event.Claim(harvest);
            Meta.Event.Claim(harvest);
            Meta.Save();
            _app.LevelSelect.Show(LobbyPage.Home);
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01n_ana_ekran_etkinlik");
            _app.OpenEvent();
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01o_etkinlik");
            _app.Modal.Close();

            long raid = (long)(new System.DateTime(2027, 2, 11, 12, 0, 0) - new System.DateTime(1970, 1, 1)).TotalSeconds;
            Meta.Clock = () => raid;
            for (int i = 0; i < 8; i++) Meta.Event.ReportWin(3, false, raid);
            Meta.Save();
            _app.OpenEvent();
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01p_etkinlik_sprint");
            _app.Modal.Close();

            long race = RaceRules.WeekStart(RaceRules.WeekOf(real)) + 2 * EconomyRules.SecondsPerDay + 18 * 3600;
            Meta.Clock = () => race;
            for (int i = 0; i < Meta.ContestRules.FromWins[1]; i++) LevelProgress.SaveStars(_app.Levels[i].Id, 3);
            Meta.Contests.Begin(ContestKind.Lantern, race - EconomyRules.SecondsPerDay);
            Meta.Contests.Begin(ContestKind.Rush, race - 1700);
            for (int i = 0; i < 3; i++) Meta.Contests.ReportWin(race - 1200 + i * 300);
            Meta.Save();
            _app.ShowLevelSelect();
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01u_ana_ekran_yarislar");
            _app.OpenContest(ContestKind.Rush);
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01v_kivilcim_kosusu");
            _app.Modal.Close();
            _app.OpenContest(ContestKind.Lantern);
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01w_fener_yarisi");
            _app.Modal.Close();
            _app.OpenLadder();
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01x_kor_merdiveni");
            _app.Modal.Close();
            for (int i = 0; i < 2; i++) Meta.Contests.ReportWin(race);
            Meta.Save();
            _app.OpenContest(ContestKind.Rush);
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01y_kosu_bitti");
            _app.Modal.Close();
            Meta.Team.Join(2);
            Meta.Contests.TeamWins(race);
            Meta.Economy.Profile.ChestWins = 12;
            Meta.Save();
            _app.LevelSelect.Show(LobbyPage.Team);
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01z_takim_sandigi");
            Meta.Team.Leave();
            StarsBackup.Restore(new System.Collections.Generic.Dictionary<int, int>());
            _app.ShowLevelSelect();
            for (int i = 0; i < 4; i++) yield return null;

            Meta.Clock = Meta.WallClock;
            var select = _app.LevelSelect;
            int district = select.RestoreFocus;
            var profile = Meta.Economy.Profile;
            profile.RestoreLevels[district] = 4;
            profile.RestoreStars[district] = 9;
            profile.RestoreBoosters[district] = 2;
            profile.RestoreClaimed[district] = 0;
            Meta.Save();
            select.ShowMap(false);
            select.Diorama.Show(district);
            select.Lobby.Refresh();
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01q_onarim_donmus");
            _app.OpenRestore();
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01r_onarim_penceresi");
            _app.Modal.Close();
            _app.Repair(district, 0);
            yield return new WaitForSecondsRealtime(0.55f);
            yield return Shot("01s_onarim_cozuluyor");
            while (select.Diorama.Busy) yield return null;
            _app.Rewards?.Close();
            _app.Repair(district, 2);
            while (select.Diorama.Busy) yield return null;
            _app.Rewards?.Close();
            select.Lobby.Refresh();
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("01t_onarim_ilerleme");

            Meta.Clock = Meta.WallClock;
            Meta.Economy.Profile.Lives = Meta.Rules.MaxLives;
            Meta.Save();
            _app.LevelSelect.ShowMap(false);
            yield return null;
            _app.OpenShop();
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01c_magaza");
            _app.Modal.Close();
            for (int i = 0; i < 7; i++) Meta.Economy.ReportStoreWin(Meta.Now);
            Meta.Economy.ClaimPass(1, false, Meta.Now);
            Meta.Save();
            _app.OpenPass();
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01c2_kor_yolu");
            _app.Modal.Close();
            _app.OpenStarter();
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01c3_baslangic_paketi");
            _app.Modal.Close();
            _app.OpenLives();
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01d_canlar");
            _app.Modal.Close();
            _app.OpenQuests();
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01e_gorevler");
            _app.Modal.Close();
            _app.OpenDaily();
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01f_gunluk_hediye");
            _app.Modal.Close();
            _app.ShowRewards("Hediye", Meta.Rules.DailyGifts[6], false);
            yield return GameTime(Timing.ModalIn + Timing.StarFill * 4f);
            yield return Shot("01g_odul");
            _app.Rewards.Close();
            yield return GameTime(RewardFlyers.Burst + RewardFlyers.Flight * 0.45f);
            yield return Shot("01g2_odul_ucuyor");
            while (_app.Flyers.Flying > 0) yield return null;
            _app.ShowRewards("Sandık", new[] { Reward.Coins(250), Reward.Of(BoosterType.Hammer) }, true);
            yield return GameTime(RewardPopup.ChestTime - RewardPopup.ChestShake * 0.4f);
            yield return Shot("01g3_sandik_sallaniyor");
            yield return GameTime(RewardPopup.ChestShake * 0.4f + 0.2f);
            yield return Shot("01g4_sandik_acildi");
            yield return GameTime(0.6f);
            _app.Rewards.Close();
            while (_app.Flyers.Flying > 0) yield return null;
            Meta.Economy.AddCoins(Meta.Rules.CoinCap);
            Meta.Save();
            yield return null;
            yield return Shot("01g5_altin_dolu");
            Meta.Economy.SpendCoins(Meta.Economy.Coins - 2000);
            Meta.Save();
            _app.ShowLevelStart(8);
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01b_seviye_basi");
            _app.LevelStart.Close();
            _app.ShowLevelStart(_app.Levels.ToList().FindIndex(l => l.Cells.Any(c => c.Frost)));
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01b2_seviye_basi_kiragi");
            _app.LevelStart.Close();
            GameApp.StreakBoosts = true;
            Meta.Economy.Profile.WinStreak = 3;
            _app.ShowLevelStart(19);
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01b3_seviye_basi_seri_super_zor");
            _app.LevelStart.Close();
            Meta.Economy.Profile.WinStreak = 0;
            GameApp.StreakBoosts = false;

            yield return Begin(_app.Levels[0]);
            yield return Shot("02_tahta_ve_hud");
            _app.Game.Hud.Emby.SetMoves(1);
            yield return GameTime(0.3f);
            yield return Shot("02e_emby_panik");
            _app.Game.Hud.Emby.React(EmbyMood.Wow);
            yield return GameTime(0.2f);
            yield return Shot("02f_emby_hayran");
            var tone = BoardSkin.Tone;
            foreach (BoardTone t in System.Enum.GetValues(typeof(BoardTone)))
            {
                BoardSkin.Tone = t;
                yield return Begin(_app.Levels[8]);
                yield return Shot("02t_ton_" + t.ToString().ToLowerInvariant());
                yield return Fire("-", "02t_ton_" + t.ToString().ToLowerInvariant() + "_roket");
                yield return Fire("*", "02t_ton_" + t.ToString().ToLowerInvariant() + "_bomba");
            }
            BoardSkin.Tone = tone;
            var firstMove = Game.Session.FindValidMoves().First(m => m.Type == MoveType.Swap);
            Game.OnTap(firstMove.A);
            yield return null;
            yield return Shot("03_secili_tas");
            Game.OnTap(firstMove.A);

            yield return Begin(Level(20, null, Even, Odd, Even, Odd, Even));
            Time.timeScale = SlowMotion;
            Game.RequestSwap(new GridPos(1, 1), new GridPos(2, 1));
            yield return GameTime(Timing.Swap * 0.6f);
            yield return Shot("04_takas_gidis");
            yield return GameTime(Timing.Swap * 0.8f);
            yield return Shot("05_takas_geri_donus");
            Time.timeScale = 1f;
            yield return WaitIdle();

            yield return Begin(Level(20, null, "BPOGY", "OGYPO", "RRBRY", "OGRPO", "YPOGY", "OGYPO", "YPOGY"));
            Time.timeScale = SlowMotion;
            Game.RequestSwap(new GridPos(2, 3), new GridPos(2, 4));
            yield return GameTime(Timing.Swap + Timing.Pop * 0.5f);
            yield return Shot("06_patlama_ve_ozel_tas_olusumu");
            yield return GameTime(Timing.SpecialForm + 0.1f);
            yield return Shot("07_dusme_ve_yeni_tas");
            Time.timeScale = 1f;
            yield return WaitIdle();

            yield return Fire("-", "08_roket_ates");
            yield return Fire("*", "09_bomba_ates");
            yield return Fire("@", "10_renk_bombasi_ates", new GridPos(3, 3));
            yield return Fire("^", "11_gudumlu_ates");
            yield return Smash(new[] { "YPOGYO", "OGYPOG", "YPOGYO", "-1111O", "YPOGYO", "OGYPOG", "YPOGYO" }, null, "11b_kutular_parcalaniyor");
            yield return Smash(new[] { "YPOGYO", "OGYPOG", "YPOGYO", "-POGYO", "YPOGYO", "OGYPOG", "YPOGYO" },
                new[] { "......", "......", "......", ".11111", "......", "......", "......" }, "11c_buz_parcalaniyor");
            BoosterInventory.Set(BoosterType.ColorBrush, 3);
            yield return Begin(Level(20, null, "YPOGYO", "OGYPOG", "YPOGYO", "OGYPOG", "YPOGYO", "OGYPOG", "YPOGYO"));
            Time.timeScale = SlowMotion;
            Game.PickBooster(BoosterType.ColorBrush);
            Game.OnTap(new GridPos(0, 0));
            yield return GameTime(0.3f);
            yield return Shot("11d_firca");
            Time.timeScale = 1f;
            yield return WaitIdle();
            Assert.IsNull(Game.LastMismatch);

            yield return Begin(Level(20, null, "YPOGYO", "OGYPOG", "YP-*YO", "OGYPOG", "YPOGYO", "OGYPOG", "YPOGYO"));
            yield return Shot("12_ozel_taslar_tahtada");
            Time.timeScale = SlowMotion;
            Game.RequestSwap(new GridPos(2, 4), new GridPos(3, 4));
            yield return GameTime(Timing.Swap + 0.1f);
            yield return Shot("13_combo_roket_bomba");
            yield return GameTime(Timing.PraisePop);
            yield return Shot("13b_ovgu_muhtesem");
            Time.timeScale = 1f;
            yield return WaitIdle();

            yield return Begin(_app.Levels[2]);
            yield return Shot("14_kutulu_seviye");
            yield return Begin(_app.Levels[3]);
            yield return Shot("15_zeminli_seviye");
            yield return Begin(_app.Levels[4]);
            yield return Shot("16_delikli_seviye");
            yield return Begin(_app.Levels[8]);
            yield return Shot("17_uc_hedefli_buzlu_seviye");
            yield return Begin(_app.Levels[6]);
            yield return Shot("18_toplanan_obje_seviyesi");

            var frosted = LevelParser.FromLayers(
                new[] { "YPOGYO", "OGYPOG", "YPOGYO", "OGYPOG", "RRBRYO", "OGYPOG", "YPOGYO" }, null, null, null,
                new[] { "......", "......", "......", ".11...", "......", "...11.", "......" });
            frosted.Name = "Deneme";
            frosted.Id = 99;
            frosted.Moves = 20;
            frosted.ColorCount = 5;
            frosted.Goals.Add(new GoalDefinition(GoalType.Cover));
            yield return Begin(frosted);
            yield return Shot("18b_kiragi_seviyesi");
            bool spread = false;
            Game.View.FrostSpread += _ => spread = true;
            Time.timeScale = SlowMotion;
            Game.RequestSwap(new GridPos(2, 2), new GridPos(3, 2));
            while (!spread) yield return null;
            yield return GameTime(0.06f);
            yield return Shot("18c_kiragi_yayiliyor");
            Time.timeScale = 1f;
            yield return WaitIdle();
            Assert.IsNull(Game.LastMismatch);
            yield return GameTime(GoalFlyers.Flight + 0.2f);
            yield return Shot("18d_kiragi_yayildi");

            BoosterInventory.Set(BoosterType.Hammer, 20);
            var blocks = LevelParser.FromLayers(new[] { "YPOGYO", "OGYPOG", "YPOGYO", "LFOGFL", "SSYPSS", "SSOGSS", "YPOGYO" }, null, null, null);
            blocks.Name = "Deneme";
            blocks.Id = 99;
            blocks.Moves = 20;
            blocks.ColorCount = 5;
            blocks.Goals.Add(new GoalDefinition(GoalType.Lamp));
            blocks.Goals.Add(new GoalDefinition(GoalType.Flower));
            blocks.Goals.Add(new GoalDefinition(GoalType.Statue));
            blocks.Goals.Add(new GoalDefinition(GoalType.Color, 20, GemColor.Red));
            yield return Begin(blocks);
            yield return Shot("18e_yeni_engeller");
            foreach (var p in new[] { new GridPos(0, 3), new GridPos(1, 3), new GridPos(0, 2), new GridPos(1, 2), new GridPos(0, 1) })
            {
                Game.PickBooster(BoosterType.Hammer);
                Game.OnTap(p);
                yield return WaitIdle();
            }
            Game.PickBooster(BoosterType.Hammer);
            Game.OnTap(new GridPos(4, 3));
            yield return WaitIdle();
            yield return Shot("18f_kandil_yandi_saksi_acti");
            Time.timeScale = SlowMotion;
            Game.PickBooster(BoosterType.Hammer);
            Game.OnTap(new GridPos(1, 1));
            yield return GameTime(0.25f);
            yield return Shot("18g_koylu_kurtuluyor");
            Time.timeScale = 1f;
            yield return WaitIdle();
            Assert.IsNull(Game.LastMismatch);
            yield return Begin(_app.Levels[59]);
            yield return Shot("18h_seviye_60_koylu");
            yield return Begin(_app.Levels[91]);
            yield return Shot("18i_seviye_92_dolu_tahta");

            yield return Win("19_kazandin", "19a_kutlama_havai_fisek", "19b_final_roketler");
            yield return Lose("20_kaybettin");

            yield return Shape("21_uzun_telefon_20x9", 1080, 2400, new Rect(0f, 90f, 1080f, 2220f));
            yield return Shape("22_cok_uzun_telefon_21x9", 1080, 2520, new Rect(0f, 90f, 1080f, 2330f));
            yield return Shape("23_kisa_telefon_16x9", 720, 1280, null);
            yield return Shape("24_tablet_3x4", 1536, 2048, null);
        }

        IEnumerator Fire(string token, string shot, GridPos? cell = null)
        {
            var p = cell ?? new GridPos(2, 3);
            string[] rows = { "YPOGYO", "OGYPOG", "YPOGYO", "OGYPOG", "YPOGYO", "OGYPOG", "YPOGYO" };
            int row = rows.Length - 1 - p.Y;
            rows[row] = rows[row].Substring(0, p.X) + token + rows[row].Substring(p.X + 1);
            yield return Begin(Level(20, null, rows));
            Time.timeScale = SlowMotion;
            Game.OnTap(p);
            yield return GameTime(0.13f);
            yield return Shot(shot);
            yield return GameTime(0.12f);
            yield return Shot(shot + "_sonra");
            Time.timeScale = 1f;
            yield return WaitIdle();
            Assert.IsNull(Game.LastMismatch);
        }

        IEnumerator Smash(string[] board, string[] cover, string shot)
        {
            var level = LevelParser.FromLayers(board, null, cover, null);
            level.Name = "Deneme";
            level.Id = 99;
            level.Moves = 20;
            level.ColorCount = 5;
            level.Goals.Add(new GoalDefinition(GoalType.Color, 40, GemColor.Red));
            yield return Begin(level);
            Time.timeScale = SlowMotion;
            Game.OnTap(new GridPos(0, 3));
            yield return GameTime(0.22f);
            yield return Shot(shot);
            yield return GameTime(0.2f);
            yield return Shot(shot + "_sonra");
            Time.timeScale = 1f;
            yield return WaitIdle();
            Assert.IsNull(Game.LastMismatch);
        }

        IEnumerator Win(string shot, string celebrationShot = null, string finaleShot = null)
        {
            yield return Begin(Level(9, new GoalDefinition(GoalType.Color, 3, GemColor.Red), "YPOGY", "OGYPO", "RRBRY", "OGRPO", "YPOGY"));
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            if (celebrationShot != null)
            {
                yield return GameTime(Timing.WinDelay + Timing.WinSparkFlight + Timing.WinSparkInterval * 3f);
                yield return Shot(celebrationShot);
            }
            if (finaleShot != null)
            {
                while (!Game.Session.FinalePlayed && Game.Popup == null) yield return null;
                if (Game.Session.FinalePlayed)
                {
                    yield return GameTime(0.3f);
                    yield return Shot(finaleShot);
                }
            }
            while (Game.Popup == null) yield return null;
            yield return GameTime(Timing.ModalIn + Timing.StarFill * 3f + 0.3f);
            yield return Shot(shot);
        }

        IEnumerator Lose(string shot)
        {
            yield return Begin(Level(1, null, "YPOGY", "OGYPO", "RRBRY", "OGRPO", "YPOGY"));
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return GameTime(Timing.LoseDelay + Timing.ModalIn + 0.2f);
            yield return Shot(shot);
        }

        IEnumerator Shape(string name, int width, int height, Rect? safeArea)
        {
            UseScreen(width, height, safeArea);
            yield return null;
            _app.ShowLevelSelect();
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot(name + "_seviye_secimi");
            yield return Begin(_app.Levels[8]);
            yield return Shot(name + "_oyun");
            yield return Win(name + "_pencere");
        }
    }
}
