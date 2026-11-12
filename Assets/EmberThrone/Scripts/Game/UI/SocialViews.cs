using System.Collections.Generic;
using System.Text;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public static class Avatar
    {
        const string Folder = "UI/Cards/";
        const string Me = "UI/Art/mascot";

        public const string MyName = "Sen";

        public static Texture2D Art(string id)
        {
            var art = id != null ? Resources.Load<Texture2D>(Folder + id) : null;
            return art != null ? art : Resources.Load<Texture2D>(Me);
        }

        public static VisualElement Build(VisualElement parent, string id, string classes = null)
        {
            var frame = Ui.El(classes == null ? "et-avatar" : "et-avatar " + classes, parent, "Avatar");
            var art = Ui.El("et-icon et-avatar__art", frame, "Art");
            art.style.backgroundImage = new StyleBackground(Art(id));
            return frame;
        }
    }

    public sealed class RaceView
    {
        const int Podium = 3;
        const int RefreshMs = 30000;

        readonly List<VisualElement> _rows = new List<VisualElement>();
        Label _left;
        Label _mine;
        ScrollView _scroll;
        VisualElement _myRow;

        public VisualElement Root { get; private set; }
        public int RowCount => _rows.Count;
        public string LeftText => _left.text;
        public string MineText => _mine.text;
        public int MyRank { get; private set; }
        public bool Shown => !Root.ClassListContains(Ui.Hidden);

        public static RaceView Create(VisualElement parent)
        {
            var view = new RaceView();
            view.Build(parent);
            return view;
        }

        void Build(VisualElement parent)
        {
            var rules = Meta.RaceRules;
            Root = Ui.El("et-social", parent, "Race");

            var header = Ui.El("et-panel et-social__header", Root, "Header");
            var top = Ui.El("et-social__top", header, "Top");
            Ui.El("et-icon et-social__emblem et-ic--trophy", top, "Emblem");
            var texts = Ui.El("et-social__texts", top, "Texts");
            Ui.Text("Köy Ligi", "et-title et-title--small", texts, "Title");
            var chips = Ui.El("et-social__chips", texts, "Chips");
            _left = Ui.Text("", "et-chip et-caption et-social__chip", chips, "Left");
            _mine = Ui.Text("", "et-chip et-caption et-social__chip", chips, "Mine");

            var prizes = Ui.El("et-social__prizes", header, "Prizes");
            for (int i = 0; i < Podium; i++)
            {
                var prize = Ui.El("et-prize", prizes, "Prize" + (i + 1));
                Ui.El("et-icon et-prize__medal " + MedalOf(i + 1), prize);
                Ui.El("et-icon et-prize__coin et-ic--coin", prize);
                Ui.Text(CoinsOf(rules.PrizeFor(i + 1)).ToString(), "et-ink et-prize__amount", prize, "Amount");
            }

            _scroll = new ScrollView(ScrollViewMode.Vertical) { name = "Rows" };
            _scroll.AddToClassList("et-social__scroll");
            _scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(_scroll);

            Refresh();
            Meta.Changed += Refresh;
            Root.RegisterCallback<DetachFromPanelEvent>(_ => Meta.Changed -= Refresh);
            Root.schedule.Execute(() =>
            {
                if (Shown) Refresh();
            }).Every(RefreshMs);
        }

        static string MedalOf(int rank) => rank == 1 ? "et-ic--medal" : "et-ic--medal-" + rank;

        static int CoinsOf(Reward[] prize)
        {
            foreach (var reward in prize)
                if (reward.Kind == RewardKind.Coins) return reward.Amount;
            return 0;
        }

        public void Refresh()
        {
            long now = Meta.Now;
            var race = Meta.Race;
            var rows = race.Standings(now);
            _left.text = "Kalan: " + Meta.Span(race.SecondsLeft(now));

            _scroll.Clear();
            _rows.Clear();
            _myRow = null;
            foreach (var row in rows)
            {
                var e = Ui.El(row.IsMe ? "et-rank et-rank--me" : "et-rank", _scroll, "Row" + row.Rank);
                if (row.Rank <= Podium) Ui.El("et-icon et-rank__medal " + MedalOf(row.Rank), e, "Medal");
                else Ui.Text(row.Rank.ToString(), "et-ink et-rank__place", e, "Place");
                Avatar.Build(e, row.Avatar);
                Ui.Text(row.IsMe ? Avatar.MyName : row.Name, "et-rank__name", e, "Name");
                Ui.El("et-icon et-rank__trophy et-ic--trophy", e);
                Ui.Text(row.Points.ToString(), "et-ink et-rank__points", e, "Points");
                _rows.Add(e);
                if (!row.IsMe) continue;
                _myRow = e;
                MyRank = row.Rank;
                _mine.text = "Sıran: " + row.Rank;
            }
        }

        public void ShowMine()
        {
            if (_myRow == null) return;
            var mine = _myRow;
            _scroll.schedule.Execute(() =>
            {
                if (Ui.Alive(mine)) _scroll.ScrollTo(mine);
            });
        }
    }

    public sealed class TeamView
    {
        const int TickMs = 1000;

        readonly List<Button> _joins = new List<Button>();
        readonly List<Button> _helps = new List<Button>();
        readonly List<VisualElement> _members = new List<VisualElement>();
        GameApp _app;
        VisualElement _body;
        Label _status;
        string _shape;

        public VisualElement Root { get; private set; }
        public Button RequestButton { get; private set; }
        public Button LeaveButton { get; private set; }
        public IReadOnlyList<Button> JoinButtons => _joins;
        public IReadOnlyList<Button> HelpButtons => _helps;
        public int MemberRows => _members.Count;
        public string StatusText => _status != null ? _status.text : "";
        public string RequestLabel => RequestButton != null ? Rows.LabelOf(RequestButton) : "";
        public bool Shown => !Root.ClassListContains(Ui.Hidden);

        public static TeamView Create(VisualElement parent, GameApp app)
        {
            var view = new TeamView { _app = app };
            view.Build(parent);
            return view;
        }

        void Build(VisualElement parent)
        {
            Root = Ui.El("et-social", parent, "Team");
            _body = Ui.El("et-social__body", Root, "Body");
            Refresh();
            Meta.Changed += Refresh;
            Root.RegisterCallback<DetachFromPanelEvent>(_ => Meta.Changed -= Refresh);
            Root.schedule.Execute(() =>
            {
                if (Shown) Tick();
            }).Every(TickMs);
        }

        string ShapeNow(long now)
        {
            var team = Meta.Team;
            var shape = new StringBuilder();
            shape.Append(team.Index).Append('|').Append(team.RequestReceived(now)).Append('|');
            foreach (var ask in team.Asks(now)) shape.Append(ask.Id).Append(':').Append(ask.Received).Append(ask.Helped ? 'h' : 'o').Append('|');
            return shape.ToString();
        }

        void Tick()
        {
            long now = Meta.Now;
            if (ShapeNow(now) != _shape) Refresh();
            else UpdateRequest(now);
        }

        public void Refresh()
        {
            long now = Meta.Now;
            _shape = ShapeNow(now);
            _body.Clear();
            _joins.Clear();
            _helps.Clear();
            _members.Clear();
            RequestButton = null;
            LeaveButton = null;
            _status = null;
            if (Meta.Team.Joined) BuildTeam(now);
            else BuildList(now);
        }

        void BuildList(long now)
        {
            var team = Meta.Team;
            var header = Ui.El("et-panel et-social__header", _body, "Header");
            var top = Ui.El("et-social__top", header, "Top");
            Ui.El("et-icon et-social__emblem et-ic--team", top, "Emblem");
            var texts = Ui.El("et-social__texts", top, "Texts");
            Ui.Text("Takımlar", "et-title et-title--small", texts, "Title");
            Ui.Text("Katıl, can iste, can gönder.", "et-caption et-ink et-ink--muted et-social__motto", texts, "Hint");

            var scroll = Scroll("Teams");
            for (int i = 0; i < team.Rules.Teams.Length; i++)
            {
                int index = i;
                var def = team.Rules.Teams[i];
                var row = Ui.El("et-row et-row--tall", scroll, "Team" + (i + 1));
                Avatar.Build(row, def.Emblem, "et-avatar--row");
                var names = Ui.El("et-row__texts", row);
                Ui.Text(def.Name, "et-row__title", names, "Title");
                Ui.Text(team.MemberCount(i) + "/" + team.Rules.Capacity + " üye · " + team.PointsOf(i, now) + " kupa", "et-caption et-row__sub", names, "Sub");
                _joins.Add(Rows.Price(row, "Katıl", null, ButtonKind.Cta, () => Join(index), "Join"));
            }
        }

        void BuildTeam(long now)
        {
            var team = Meta.Team;
            var def = team.Current;
            var header = Ui.El("et-panel et-social__header", _body, "Header");
            var top = Ui.El("et-social__top", header, "Top");
            Avatar.Build(top, def.Emblem, "et-avatar--big");
            var texts = Ui.El("et-social__texts", top, "Texts");
            Ui.Text(def.Name, "et-title et-title--small", texts, "Title");
            Ui.Text(def.Motto, "et-caption et-ink et-ink--muted et-social__motto", texts, "Motto");
            var chips = Ui.El("et-social__chips", texts, "Chips");
            Ui.Text(team.MemberCount(team.Index) + "/" + team.Rules.Capacity + " üye", "et-chip et-caption et-social__chip", chips, "Size");
            Ui.Text(team.PointsOf(team.Index, now) + " kupa", "et-chip et-caption et-social__chip", chips, "Points");

            var request = Ui.El("et-social__request", header, "Request");
            RequestButton = Rows.Price(request, "Can iste", "et-ic--heart", ButtonKind.Cta, Request, "Ask");
            _status = Ui.Text("", "et-caption et-ink et-social__status", request, "Status");
            LeaveButton = Rows.Price(request, "Ayrıl", null, ButtonKind.Secondary, Leave, "Leave");
            LeaveButton.AddToClassList("et-social__leave");
            UpdateRequest(now);

            var scroll = Scroll("Feed");
            foreach (var ask in team.Asks(now))
            {
                int id = ask.Id;
                var row = Ui.El("et-row", scroll, "Ask" + ask.Id);
                Avatar.Build(row, ask.Avatar, "et-avatar--row");
                var names = Ui.El("et-row__texts", row);
                Ui.Text(ask.Name, "et-row__title", names, "Title");
                Ui.Text("Can istiyor · " + ask.Received + "/" + ask.Needed + " · " + Meta.Span(ask.SecondsLeft), "et-caption et-row__sub", names, "Sub");
                if (ask.Helped) Ui.El("et-check et-row__check", row, "Sent");
                else _helps.Add(Rows.Price(row, "Gönder", "et-ic--heart", ButtonKind.Cta, () => Help(id), "Help"));
            }

            var members = team.Members(now);
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                var e = Ui.El(member.IsMe ? "et-rank et-rank--me" : "et-rank", scroll, "Member" + (i + 1));
                Ui.Text((i + 1).ToString(), "et-ink et-rank__place", e, "Place");
                Avatar.Build(e, member.Avatar);
                Ui.Text(member.IsMe ? Avatar.MyName : member.Name, "et-rank__name", e, "Name");
                Ui.El("et-icon et-rank__trophy et-ic--heart", e);
                Ui.Text(member.Helps.ToString(), "et-ink et-rank__points et-rank__points--short", e, "Helps");
                Ui.El("et-icon et-rank__trophy et-ic--trophy", e);
                Ui.Text(member.Points.ToString(), "et-ink et-rank__points", e, "Points");
                _members.Add(e);
            }
        }

        ScrollView Scroll(string name)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = name };
            scroll.AddToClassList("et-social__scroll");
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _body.Add(scroll);
            return scroll;
        }

        void UpdateRequest(long now)
        {
            if (RequestButton == null) return;
            var team = Meta.Team;
            bool can = team.CanRequest(now);
            RequestButton.SetEnabled(can);
            int needed = team.Rules.LivesPerRequest;
            int received = team.RequestReceived(now);
            if (team.RequestOpen(now) && received < needed) _status.text = "Yolda: " + received + "/" + needed + " can";
            else if (team.RequestCooldown(now) > 0) _status.text = "Yeni istek: " + Meta.Span(team.RequestCooldown(now));
            else if (Meta.Economy.LivesFull) _status.text = "Canın dolu";
            else _status.text = "Takımın " + needed + " can gönderir";
        }

        void Join(int index)
        {
            if (!Meta.Team.Join(index)) return;
            Sound.Play(Sfx.Collect);
            Meta.Save();
        }

        void Request()
        {
            if (!Meta.Team.Request(Meta.Now)) return;
            Sound.Play(Sfx.Collect);
            Meta.Save();
        }

        void Help(int ask)
        {
            int coins = Meta.Team.Help(ask, Meta.Now);
            if (coins <= 0) return;
            Sound.Play(Sfx.Collect);
            Meta.Save();
        }

        void Leave()
        {
            var def = Meta.Team.Current;
            if (def == null) return;
            _app.Confirm("Takımdan ayrıl", def.Name + " takımından ayrılmak istiyor musun?", "Ayrıl", "Vazgeç", () =>
            {
                if (Meta.Team.Leave()) Meta.Save();
            });
        }
    }
}
