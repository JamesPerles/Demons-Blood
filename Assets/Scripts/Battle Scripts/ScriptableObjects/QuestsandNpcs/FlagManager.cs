using System;
using System.Collections.Generic;
using UnityEngine;

public class FlagManager : MonoBehaviour
{
public static FlagManager instance;
Dictionary<string, bool> flags = new Dictionary<string, bool>();
public event Action<string> onFlagChanged;
void Awake()
    {
        if(instance == null){instance = this; DontDestroyOnLoad(gameObject);}
        else Destroy(gameObject);
    }
    public void SetFlag(string key, bool value = true)
    {
        if(string.IsNullOrEmpty(key)) return;
        flags[key] = value;
        onFlagChanged?.Invoke(key);
    }
    public bool GetFlags(string key)
    {
        return !string.IsNullOrEmpty(key) && flags.TryGetValue(key, out bool value) && value;
    }
    public Dictionary<string, bool> SaveFlags() => new Dictionary<string, bool>(flags);
    public void LoadFlags(Dictionary<string, bool> restored) {flags = new Dictionary<string, bool>(restored);}

 }
