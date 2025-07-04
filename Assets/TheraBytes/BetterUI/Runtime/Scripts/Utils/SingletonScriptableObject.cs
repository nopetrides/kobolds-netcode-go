﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    public abstract class SingletonScriptableObject<T> : ScriptableObject
        where T : SingletonScriptableObject<T>
    {
        static T instance;
        static object instanceLock = new object();

        public static T Instance
        {
            get
            {
                EnsureInstance();
                return instance;
            }
        }

        public static bool HasInstance { get { return instance != null; } }

        public static bool ScriptableObjectFileExists
        {
            get
            {
                if (HasInstance)
                    return true;

                string filePath = GetFilePathWithExtention(true);
                return System.IO.File.Exists(filePath);
            }
        }

        public static T EnsureInstance()
        {
            if (instance != null)
                return instance;

            lock (instanceLock)
            {
                if (instance != null)
                    return instance;

                string filePath = GetFilePathWithExtention(false);

                string resourceFilePath = Path.GetFileNameWithoutExtension(
                        filePath.Split(new string[] { "Resources" }, StringSplitOptions.None).Last());

                var obj = Resources.Load(resourceFilePath);
                instance = obj as T; // note: in the debugger it might be displayed as null (which is not the case)

                if (obj == null)
                {
#if UNITY_EDITOR && !UNITY_CLOUD_BUILD
                    string completeFilePath = Path.Combine(Application.dataPath, filePath);
                    string assetPath = "Assets/" + filePath;

                    // when upgrading unity, the asset cannot be loaded through resources.
                    // to prevent a recreation / reset of the asset, load it through the asset database (which works)
                    if (File.Exists(completeFilePath))
                    {
                        instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    }
                    else
                    {
                        string directory = Path.GetDirectoryName(completeFilePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        instance = CreateInstance<T>();
                        UnityEditor.AssetDatabase.CreateAsset(instance, assetPath);
                        UnityEditor.AssetDatabase.Refresh();
                    }
#else
                    instance = CreateInstance<T>();
                    Debug.LogErrorFormat(
                        "Could not find scriptable object of type '{0}'. Make sure it is instantiated inside Unity before building.",
                        typeof(T));
#endif
                }
            }

            return instance;
        }

        private static string GetFilePathWithExtention(bool fullPath)
        {
            System.Type t = typeof(T);
            var prop = t.GetProperty("FilePath", BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic);

            if (prop == null)
                throw new Exception("No static Property 'FilePath' in " + t.ToString());

            string filePath = prop.GetValue(null, null) as string;

            if (filePath == null)
                throw new Exception("static property 'FilePath' is not a string or null in " + t.ToString());
            if (!filePath.Contains("Resources"))
                throw new Exception("static property 'FilePath' must contain a Resources folder.");
            if (filePath.Contains("Plugins"))
                throw new Exception("static property 'FilePath' must not contain a Plugin folder.");

            if (!filePath.EndsWith(".asset"))
                filePath += ".asset";

            return (fullPath)
                ? System.IO.Path.Combine(Application.dataPath, filePath)
                : filePath;
        }
    }
}
