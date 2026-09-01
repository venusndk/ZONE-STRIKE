using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;
using ZoneStrike.Core.EventChannels;

namespace ZoneStrike.Presentation.Audio
{
    public enum MusicState { Menu, LootPhase, ShrinkPhase, Combat }

    /// <summary>
    /// Architecture role: Presentation-layer audio playback service (GDD §24 — directional audio
    /// as "a core competitive system, not just atmosphere"). Owns pooled 3D SFX playback and the
    /// adaptive music state machine, reacting to Gameplay state via event-channel subscriptions,
    /// matching the same opt-in decoupling pattern as <c>UIManager</c> (see that class's doc
    /// comment for why this is a per-system choice, not an absolute layering rule).
    ///
    /// Optimisation notes: <see cref="AudioSource"/> instances are drawn from a
    /// <see cref="UnityEngine.Pool.ObjectPool{T}"/> (Technical Architecture §8 package choice —
    /// no third-party pooling dependency needed) rather than instantiated per SFX call, which
    /// matters given combat can trigger many simultaneous spatial SFX (weapon fire, impacts,
    /// ability casts) within the §10.1 0.8ms audio frame-time budget.
    ///
    /// Extension points: <see cref="SetMusicState"/> crossfades between pre-authored stems
    /// (GDD §24: ambient → rising → combat, always crossfaded not hard-cut) — adding a new
    /// adaptive layer (e.g. a distinct "final zone" stem) means adding one more
    /// <see cref="MusicState"/> case and a stem reference, not restructuring the crossfade logic.
    ///
    /// Networking considerations: none — purely local playback. Which SFX/music state to play is
    /// driven by already-server-validated event data (e.g. a confirmed hit, a real zone-stage
    /// change), so this system has no independent trust decisions to make.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;
        [SerializeField] private AudioMixerGroup _musicMixerGroup;
        [SerializeField] private AudioSource _musicSourceA;
        [SerializeField] private AudioSource _musicSourceB;
        [SerializeField] private float _musicCrossfadeSeconds = 2.5f;
        [SerializeField] private AudioSource _pooledSfxPrefab;
        [SerializeField] private MatchStateEventChannelSO _matchStateChannel;
        [SerializeField] private ZoneRingEventChannelSO _zoneRingChannel;

        private ObjectPool<AudioSource> _sfxPool;
        private AudioSource _activeMusicSource;
        private AudioSource _inactiveMusicSource;
        private readonly Dictionary<MusicState, AudioClip> _musicStems = new();
        private float _crossfadeElapsed;
        private bool _isCrossfading;

        private void Awake()
        {
            _sfxPool = new ObjectPool<AudioSource>(
                createFunc: CreatePooledSfxSource,
                actionOnGet: source => source.gameObject.SetActive(true),
                actionOnRelease: source => source.gameObject.SetActive(false),
                actionOnDestroy: source => Destroy(source.gameObject),
                collectionCheck: false,
                defaultCapacity: 16,
                maxSize: 32);

            _activeMusicSource = _musicSourceA;
            _inactiveMusicSource = _musicSourceB;
        }

        private void OnEnable()
        {
            _matchStateChannel?.Subscribe(OnMatchStateChanged);
            _zoneRingChannel?.Subscribe(OnZoneRingChanged);
        }

        private void OnDisable()
        {
            _matchStateChannel?.Unsubscribe(OnMatchStateChanged);
            _zoneRingChannel?.Unsubscribe(OnZoneRingChanged);
        }

        private AudioSource CreatePooledSfxSource()
        {
            var instance = Instantiate(_pooledSfxPrefab, transform);
            instance.outputAudioMixerGroup = _sfxMixerGroup;
            return instance;
        }

        /// <summary>Plays a one-shot spatial SFX at a world position, releasing the pooled source back automatically when finished.</summary>
        public void PlaySfx(AudioClip clip, Vector3 worldPosition)
        {
            if (clip == null) return;

            var source = _sfxPool.Get();
            source.transform.position = worldPosition;
            source.clip = clip;
            source.Play();

            StartCoroutine(ReleaseAfterPlayback(source, clip.length));
        }

        private System.Collections.IEnumerator ReleaseAfterPlayback(AudioSource source, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            _sfxPool.Release(source);
        }

        public void RegisterMusicStem(MusicState state, AudioClip clip) => _musicStems[state] = clip;

        public void SetMusicState(MusicState state)
        {
            if (!_musicStems.TryGetValue(state, out var clip) || clip == null) return;
            if (_activeMusicSource.clip == clip) return;

            _inactiveMusicSource.clip = clip;
            _inactiveMusicSource.volume = 0f;
            _inactiveMusicSource.Play();

            _crossfadeElapsed = 0f;
            _isCrossfading = true;
        }

        private void Update()
        {
            if (!_isCrossfading) return;

            _crossfadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_crossfadeElapsed / _musicCrossfadeSeconds);

            _activeMusicSource.volume = 1f - t;
            _inactiveMusicSource.volume = t;

            if (t >= 1f)
            {
                _activeMusicSource.Stop();
                (_activeMusicSource, _inactiveMusicSource) = (_inactiveMusicSource, _activeMusicSource);
                _isCrossfading = false;
            }
        }

        private void OnMatchStateChanged(MatchStateChangedPayload payload)
        {
            switch (payload.NewState)
            {
                case MatchState.Drop:
                case MatchState.ZoneHold:
                    SetMusicState(MusicState.LootPhase);
                    break;
                case MatchState.FinalZone:
                case MatchState.SuddenDeath:
                    SetMusicState(MusicState.Combat);
                    break;
            }
        }

        private void OnZoneRingChanged(ZoneRingChangedPayload payload)
        {
            if (payload.IsShrinking) SetMusicState(MusicState.ShrinkPhase);
        }
    }
}
