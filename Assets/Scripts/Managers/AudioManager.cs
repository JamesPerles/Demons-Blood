using UnityEngine;

public class AudioManager : MonoBehaviour
{
   public static AudioManager instance;
   public AudioSource musicSource;
   public AudioSource sfxSource;
   void Awake()
    {
        if(instance == null) {instance = this; DontDestroyOnLoad(gameObject);}
    else{Destroy(gameObject); return;}
    if(musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
    if(sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
    musicSource.loop = true;
    musicSource.playOnAwake = false;
    sfxSource.playOnAwake = false;
    }
    void Start()
    {
        if(SettingsManager.instance != null)
        {
            musicSource.volume = SettingsManager.instance.musicVolume;
            sfxSource.volume = SettingsManager.instance.sfxVolume;
        }
    }
    void OnEnable()
    {
        if(SettingsManager.instance != null)
        {
            SettingsManager.instance.OnMusicVolumeChanged += MusicVolumeChanged;
            SettingsManager.instance.OnSfxVolumeChanged += SfxVolumeChanged;
        }
    }
    void OnDisable()
    {
        if(SettingsManager.instance != null)
        {
            SettingsManager.instance.OnMusicVolumeChanged -= MusicVolumeChanged;
            SettingsManager.instance.OnSfxVolumeChanged -= SfxVolumeChanged;
        }
    }
    void MusicVolumeChanged(float volume) => musicSource.volume = volume;
    void SfxVolumeChanged(float volume) => sfxSource.volume = volume;
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if(clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }
    public void StopMusic() => musicSource.Stop();
    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();
    public void PlaySFX(AudioClip clip)
    {
        if(clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
