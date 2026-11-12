using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace EmberThrone.Game
{
    public sealed class VideoView
    {
        const string Folder = "Video/";
        const string Posters = "UI/Art/";
        const long RevealDelayMs = 60;

        GameObject _host;
        VideoPlayer _player;
        RenderTexture _texture;
        VisualElement _screen;

        public VisualElement Root { get; private set; }
        public bool HasClip { get; private set; }
        public bool Started { get; private set; }
        public bool Finished { get; private set; }
        public bool Failed { get; private set; }
        public float Length { get; private set; }

        public static bool Supported => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        public static VideoView Create(VisualElement parent, Transform owner, string name, bool loop, bool sound)
        {
            var view = new VideoView();
            view.Build(parent, owner, name, loop, sound);
            return view;
        }

        void Build(VisualElement parent, Transform owner, string name, bool loop, bool sound)
        {
            Root = Ui.El("et-fill et-video", parent, "Video");
            var poster = Resources.Load<Texture2D>(Posters + name);
            if (poster != null) Root.style.backgroundImage = new StyleBackground(poster);
            _screen = Ui.El("et-fill et-video__screen " + Ui.Hidden, Root, "Screen");
            Root.RegisterCallback<DetachFromPanelEvent>(_ => Dispose());

            var clip = Resources.Load<VideoClip>(Folder + name);
            HasClip = clip != null;
            if (!HasClip || !Supported)
            {
                Failed = true;
                return;
            }
            Length = (float)clip.length;

            _texture = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = "Video " + name };
            _texture.Create();
            _host = new GameObject("Video " + name);
            _host.transform.SetParent(owner, false);
            _player = _host.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.source = VideoSource.VideoClip;
            _player.clip = clip;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.targetTexture = _texture;
            _player.isLooping = loop;
            _player.skipOnDrop = true;
            _player.waitForFirstFrame = true;
            bool audio = sound && Sound.Enabled && clip.audioTrackCount > 0;
            _player.audioOutputMode = audio ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;
            if (audio)
            {
                _player.controlledAudioTrackCount = 1;
                _player.EnableAudioTrack(0, true);
                _player.SetDirectAudioVolume(0, 1f);
            }
            _player.prepareCompleted += p => p.Play();
            _player.started += _ => Reveal();
            _player.loopPointReached += _ =>
            {
                if (!loop) Finished = true;
            };
            _player.errorReceived += (_, message) =>
            {
                Failed = true;
                Debug.LogWarning("[EmberThrone] Video " + name + " could not play: " + message);
            };
            _screen.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_texture));
            _player.Prepare();
        }

        void Reveal()
        {
            Started = true;
            var screen = _screen;
            screen.schedule.Execute(() =>
            {
                if (Ui.Alive(screen)) screen.RemoveFromClassList(Ui.Hidden);
            }).ExecuteLater(RevealDelayMs);
        }

        public void Dispose()
        {
            if (_player != null) _player.Stop();
            if (_host != null) UnityEngine.Object.Destroy(_host);
            if (_texture != null)
            {
                _texture.Release();
                UnityEngine.Object.Destroy(_texture);
            }
            _host = null;
            _player = null;
            _texture = null;
        }
    }

    public sealed class StoryScene
    {
        const float FadeIn = 0.35f;
        const float StillSeconds = 3.5f;
        const float EndPause = 0.8f;
        const float Margin = 3f;
        const int TickMs = 50;

        Action<bool> _closed;
        float _time;
        float _endedAt = -1f;

        public VisualElement Root { get; private set; }
        public VideoView Video { get; private set; }
        public Button SkipButton { get; private set; }
        public int Chapter { get; private set; }
        public bool IsOpen => Root != null && Root.parent != null;

        public static StoryScene Show(VisualElement parent, Transform owner, int chapter, Action<bool> closed)
        {
            var scene = new StoryScene { Chapter = chapter, _closed = closed };
            scene.Build(parent, owner);
            return scene;
        }

        void Build(VisualElement parent, Transform owner)
        {
            var district = LevelMap.Chapters[Chapter];
            Root = Ui.El("et-fill et-story", parent, "Story");
            Root.pickingMode = PickingMode.Position;
            Video = VideoView.Create(Root, owner, LevelMap.SceneOf(Chapter), false, true);
            Ui.Text(district.Title, "et-chip et-outlined et-story__title", Root, "Title");
            Ui.Text(district.Story, "et-chip et-caption et-story__caption", Root, "Caption");
            SkipButton = Ui.Button("Geç", Ui.ClassOf(ButtonKind.Secondary) + " et-btn--small et-story__skip", () => Close(), Root, "Skip");
            Root.style.opacity = 0f;
            Root.schedule.Execute(Tick).Every(TickMs).Until(() => !IsOpen);
        }

        void Tick(TimerState state)
        {
            _time += state.deltaTime / 1000f;
            Root.style.opacity = Mathf.Clamp01(_time / FadeIn);
            if (Video.Finished && _endedAt < 0f) _endedAt = _time;
            float limit = Video.Failed && !Video.Started ? StillSeconds : Video.Length + Margin;
            if ((_endedAt >= 0f && _time - _endedAt >= EndPause) || _time >= limit) Close();
        }

        public void Close(bool notify = true)
        {
            if (!IsOpen) return;
            Root.RemoveFromHierarchy();
            var closed = _closed;
            _closed = null;
            closed?.Invoke(notify);
        }
    }
}
