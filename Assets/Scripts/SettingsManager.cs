using UnityEngine;
public class SettingsManager : MonoBehaviour
{
public static SettingsManager instance;
[Range(0f, 1f)] public float musicVolume = 1f;
[Range(0f, 1f)] public float sfxVolume = 1f;
public float dialogueTextSpeed = 40f;
public float battleTextSpeed = 40f;
[Range(0.5f, 2.5f)] public float battleSpeedMultiplier = 1f;
public Color uiTextColor = Color.white;
public event System.Action<Color> OnTextColorChanged;
public void SetTextColor(Color c) {uiTextColor = c; OnTextColorChanged?.Invoke(c);}
public Color pauseMenuPanelColor = new Color(0.05f, 0.08f, 0.2f, 0.9f);
public event System.Action<float> OnMusicVolumeChanged;
public event System.Action<float> OnSfxVolumeChanged;
public void SetMusicVolume(float volume) {musicVolume = volume; OnMusicVolumeChanged?.Invoke(volume);}
public void SetSfxVolume(float volume) {sfxVolume = volume; OnSfxVolumeChanged?.Invoke(volume);}
void Awake()
    {
        if(instance == null) {instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }
    public void SettingsSave(SettingsSaveData data)
    {
        if(data == null) return;
        SetMusicVolume (data.musicVolume);
        SetSfxVolume (data.sfxVolume);
        dialogueTextSpeed = data.dialogueTextSpeed;
        battleTextSpeed = data.battleTextSpeed;
        battleSpeedMultiplier = data.battleSpeedMultiplier;
        SetTextColor(data.textColor);
        pauseMenuPanelColor = data.pauseMenuPanelColor;
    }
}
