using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FootballGame.Audio
{
    public enum SFX
    {
        Goal, Whistle, YellowCard, RedCard, Penalty, ButtonClick,
        CoinCollect, PackOpen, MatchKickoff, HalfTime, FullTime,
        Substitution, Corner, Foul, Cheer, Boo
    }

    public enum MusicTrack { Menu, Match, Victory, Defeat }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("SFX Clips")]
        public AudioClip sfxGoal;
        public AudioClip sfxWhistle;
        public AudioClip sfxYellowCard;
        public AudioClip sfxRedCard;
        public AudioClip sfxPenalty;
        public AudioClip sfxButtonClick;
        public AudioClip sfxCoinCollect;
        public AudioClip sfxPackOpen;
        public AudioClip sfxMatchKickoff;
        public AudioClip sfxHalfTime;
        public AudioClip sfxFullTime;
        public AudioClip sfxSubstitution;
        public AudioClip sfxCorner;
        public AudioClip sfxFoul;
        public AudioClip sfxCheer;
        public AudioClip sfxBoo;

        [Header("Music Tracks")]
        public AudioClip musicMenu;
        public AudioClip musicMatch;
        public AudioClip musicVictory;
        public AudioClip musicDefeat;

        [Header("Sources")]
        public AudioSource sfxSource;
        public AudioSource musicSource;

        private Dictionary<SFX, AudioClip> _sfxMap;
        private Dictionary<MusicTrack, AudioClip> _musicMap;

        private bool _sfxEnabled = true;
        private bool _musicEnabled = true;
        private float _sfxVolume = 1f;
        private float _musicVolume = 0.6f;

        public bool SFXEnabled   => _sfxEnabled;
        public bool MusicEnabled => _musicEnabled;
        public float SFXVolume   => _sfxVolume;
        public float MusicVolume => _musicVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildMaps();
            LoadPrefs();
        }

        private void BuildMaps()
        {
            _sfxMap = new Dictionary<SFX, AudioClip>
            {
                { SFX.Goal,         sfxGoal },
                { SFX.Whistle,      sfxWhistle },
                { SFX.YellowCard,   sfxYellowCard },
                { SFX.RedCard,      sfxRedCard },
                { SFX.Penalty,      sfxPenalty },
                { SFX.ButtonClick,  sfxButtonClick },
                { SFX.CoinCollect,  sfxCoinCollect },
                { SFX.PackOpen,     sfxPackOpen },
                { SFX.MatchKickoff, sfxMatchKickoff },
                { SFX.HalfTime,     sfxHalfTime },
                { SFX.FullTime,     sfxFullTime },
                { SFX.Substitution, sfxSubstitution },
                { SFX.Corner,       sfxCorner },
                { SFX.Foul,         sfxFoul },
                { SFX.Cheer,        sfxCheer },
                { SFX.Boo,          sfxBoo },
            };

            _musicMap = new Dictionary<MusicTrack, AudioClip>
            {
                { MusicTrack.Menu,    musicMenu },
                { MusicTrack.Match,   musicMatch },
                { MusicTrack.Victory, musicVictory },
                { MusicTrack.Defeat,  musicDefeat },
            };
        }

        private void LoadPrefs()
        {
            _sfxEnabled   = PlayerPrefs.GetInt("sfx_enabled", 1) == 1;
            _musicEnabled = PlayerPrefs.GetInt("music_enabled", 1) == 1;
            _sfxVolume    = PlayerPrefs.GetFloat("sfx_volume", 1f);
            _musicVolume  = PlayerPrefs.GetFloat("music_volume", 0.6f);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (sfxSource)   sfxSource.volume   = _sfxEnabled   ? _sfxVolume   : 0f;
            if (musicSource) musicSource.volume  = _musicEnabled ? _musicVolume : 0f;
        }

        public void PlaySFX(SFX sfx)
        {
            if (!_sfxEnabled || sfxSource == null) return;
            if (_sfxMap.TryGetValue(sfx, out var clip) && clip != null)
                sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        public void PlayMusic(MusicTrack track)
        {
            if (musicSource == null) return;
            if (_musicMap.TryGetValue(track, out var clip) && clip != null)
            {
                musicSource.clip = clip;
                musicSource.loop = true;
                if (_musicEnabled) musicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void SetSFXEnabled(bool on)
        {
            _sfxEnabled = on;
            PlayerPrefs.SetInt("sfx_enabled", on ? 1 : 0);
            ApplyVolumes();
        }

        public void SetMusicEnabled(bool on)
        {
            _musicEnabled = on;
            PlayerPrefs.SetInt("music_enabled", on ? 1 : 0);
            if (musicSource != null)
            {
                if (on && !musicSource.isPlaying) musicSource.Play();
                else if (!on) musicSource.Pause();
            }
            ApplyVolumes();
        }

        public void SetSFXVolume(float v)
        {
            _sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("sfx_volume", _sfxVolume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float v)
        {
            _musicVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("music_volume", _musicVolume);
            ApplyVolumes();
        }
    }
}
