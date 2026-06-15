using System.Collections.Generic;
using UnityEngine;
using FootballGame.Core;

namespace FootballGame.Audio
{
    public enum SoundEffect
    {
        Goal, Whistle, Crowd, YellowCard, RedCard, Penalty,
        ButtonClick, MenuOpen, MenuClose, CoinCollect, PackOpen,
        MatchKickoff, HalfTime, FullTime, Substitution, Corner,
        Foul, Cheer, Boo, Tackle, Save, PostHit
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _crowdSource;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip[] _goalClips;
        [SerializeField] private AudioClip[] _whistleClips;
        [SerializeField] private AudioClip[] _crowdClips;
        [SerializeField] private AudioClip _yellowCardClip;
        [SerializeField] private AudioClip _redCardClip;
        [SerializeField] private AudioClip _penaltyWhistleClip;
        [SerializeField] private AudioClip _buttonClickClip;
        [SerializeField] private AudioClip _menuOpenClip;
        [SerializeField] private AudioClip _menuCloseClip;
        [SerializeField] private AudioClip _coinCollectClip;
        [SerializeField] private AudioClip _packOpenClip;
        [SerializeField] private AudioClip _kickoffClip;
        [SerializeField] private AudioClip _halfTimeClip;
        [SerializeField] private AudioClip _fullTimeClip;
        [SerializeField] private AudioClip _substitutionClip;
        [SerializeField] private AudioClip _cornerClip;
        [SerializeField] private AudioClip _foulClip;
        [SerializeField] private AudioClip _cheerClip;
        [SerializeField] private AudioClip _booClip;
        [SerializeField] private AudioClip _tackleClip;
        [SerializeField] private AudioClip _saveClip;
        [SerializeField] private AudioClip _postHitClip;

        [Header("Music")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField] private AudioClip _matchMusic;
        [SerializeField] private AudioClip _victoryMusic;
        [SerializeField] private AudioClip _defeatMusic;

        private bool _sfxEnabled = true;
        private bool _musicEnabled = true;
        private float _sfxVolume = 1f;
        private float _musicVolume = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }

        private void LoadSettings()
        {
            _sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
            _musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
            _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            _musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            ApplySettings();
        }

        public void PlaySFX(SoundEffect effect)
        {
            if (!_sfxEnabled || _sfxSource == null) return;

            AudioClip clip = GetClip(effect);
            if (clip != null)
                _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        public void PlayRandomSFX(SoundEffect effect)
        {
            if (!_sfxEnabled || _sfxSource == null) return;

            AudioClip[] clips = GetClips(effect);
            if (clips != null && clips.Length > 0)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                if (clip != null) _sfxSource.PlayOneShot(clip, _sfxVolume);
            }
        }

        public void PlayMusic(string musicName)
        {
            if (!_musicEnabled || _musicSource == null) return;
            AudioClip clip = GetMusicClip(musicName);
            if (clip == null) return;
            _musicSource.clip = clip;
            _musicSource.loop = true;
            _musicSource.volume = _musicVolume;
            _musicSource.Play();
        }

        public void PlayCrowdAmbience(bool excited)
        {
            if (!_sfxEnabled || _crowdSource == null) return;
            if (_crowdClips == null || _crowdClips.Length == 0) return;
            int idx = excited ? Mathf.Min(1, _crowdClips.Length - 1) : 0;
            _crowdSource.clip = _crowdClips[idx];
            _crowdSource.loop = true;
            _crowdSource.volume = _sfxVolume * 0.4f;
            if (!_crowdSource.isPlaying) _crowdSource.Play();
        }

        public void StopMusic() => _musicSource?.Stop();
        public void StopCrowd() => _crowdSource?.Stop();

        public void SetSFXEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
            PlayerPrefs.SetInt("SFXEnabled", enabled ? 1 : 0);
            if (!enabled) { _sfxSource?.Stop(); _crowdSource?.Stop(); }
        }

        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;
            PlayerPrefs.SetInt("MusicEnabled", enabled ? 1 : 0);
            if (enabled) _musicSource?.Play();
            else _musicSource?.Pause();
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
            if (_musicSource != null) _musicSource.volume = _musicVolume;
        }

        private void ApplySettings()
        {
            if (_sfxSource != null) _sfxSource.volume = _sfxVolume;
            if (_musicSource != null) _musicSource.volume = _musicVolume;
        }

        private AudioClip GetClip(SoundEffect effect)
        {
            return effect switch
            {
                SoundEffect.Goal => _goalClips?.Length > 0 ? _goalClips[0] : null,
                SoundEffect.Whistle => _whistleClips?.Length > 0 ? _whistleClips[0] : null,
                SoundEffect.YellowCard => _yellowCardClip,
                SoundEffect.RedCard => _redCardClip,
                SoundEffect.Penalty => _penaltyWhistleClip,
                SoundEffect.ButtonClick => _buttonClickClip,
                SoundEffect.MenuOpen => _menuOpenClip,
                SoundEffect.MenuClose => _menuCloseClip,
                SoundEffect.CoinCollect => _coinCollectClip,
                SoundEffect.PackOpen => _packOpenClip,
                SoundEffect.MatchKickoff => _kickoffClip,
                SoundEffect.HalfTime => _halfTimeClip,
                SoundEffect.FullTime => _fullTimeClip,
                SoundEffect.Substitution => _substitutionClip,
                SoundEffect.Corner => _cornerClip,
                SoundEffect.Foul => _foulClip,
                SoundEffect.Cheer => _cheerClip,
                SoundEffect.Boo => _booClip,
                SoundEffect.Tackle => _tackleClip,
                SoundEffect.Save => _saveClip,
                SoundEffect.PostHit => _postHitClip,
                _ => null
            };
        }

        private AudioClip[] GetClips(SoundEffect effect)
        {
            return effect switch
            {
                SoundEffect.Goal => _goalClips,
                SoundEffect.Whistle => _whistleClips,
                SoundEffect.Crowd => _crowdClips,
                _ => null
            };
        }

        private AudioClip GetMusicClip(string name)
        {
            return name switch
            {
                "menu" => _menuMusic,
                "match" => _matchMusic,
                "victory" => _victoryMusic,
                "defeat" => _defeatMusic,
                _ => null
            };
        }
    }
}
