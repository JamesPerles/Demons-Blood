using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
public class EffectAssetGenerator
{
    const string OutputFolder = "Assets/Effects";
    [MenuItem("Battle/Generate Missing Effect Assets")]
    public static void GenerateMissingEffects()
    {
        if(!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);
        var effectTypes = System.AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => typeof(Effect).IsAssignableFrom(type) && !type.IsAbstract
        && type.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false).Length == 0);
        int created = 0;
        foreach(var type in effectTypes)
        {
            string path = $"{OutputFolder}/{type.Name}.asset";
            if(AssetDatabase.LoadAssetAtPath<Effect>(path) != null) continue;
            Effect instance = ScriptableObject.CreateInstance(type) as Effect;
            AssetDatabase.CreateAsset(instance, path);
            created++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Created {created} new effect asset(s).");
    }
    }
