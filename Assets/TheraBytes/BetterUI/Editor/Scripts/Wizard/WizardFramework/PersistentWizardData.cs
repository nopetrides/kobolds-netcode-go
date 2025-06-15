using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
    public class PersistentWizardData
    {
        const char SEPARATOR = '=';

        string filePath;
        Dictionary<string, string> data;

        public int SavedDataCount { get { return (data != null) ? data.Count : 0; } }

        public PersistentWizardData(string filePath)
        {
            this.filePath = filePath;
        }

        public bool FileExists()
        {
            return System.IO.File.Exists(filePath);
        }

        public bool TryDeserialize()
        {
            if (!FileExists())
                return false;

            try
            {
                this.data = new Dictionary<string, string>();
                string[] lines = System.IO.File.ReadAllLines(filePath);
                foreach (string l in lines)
                {
                    string[] kv = l.Split(new char[] { SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
                    data.Add(kv[0], kv[1]);
                }
                return true;
            }
            catch (Exception ex)
            {
                data.Clear();
                Debug.LogError("could not deserialize wizard data: " + ex);
                return false;
            }
        }

        public bool TryGetValue(string key, out string parsableValueString)
        {
            if(data == null)
            {
                if(TryDeserialize() == false)
                {
                    parsableValueString = null;
                    return false;
                }
            }

            return data.TryGetValue(key, out parsableValueString);
        }

        public void RegisterValue(string key, string parsableValueString)
        {
            if (data == null)
            {
                if (!TryDeserialize())
                {
                    data = new Dictionary<string, string>();
                }
            }

            data[key] = parsableValueString;
        }

        public bool RemoveEntry(string key)
        {
            if (data == null)
            {
                if (!TryDeserialize())
                {
                    return false;
                }
            }

            return data.Remove(key);
        }


        public void Save()
        {
            // ensure the directory exists
            var dir = System.IO.Path.GetDirectoryName(filePath);
            if(!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            // ensure that there is no old data at the end of the file after save.
            if(FileExists())
            {
                System.IO.File.Delete(filePath);
            }

            // save the data
            using(System.IO.FileStream stream = System.IO.File.OpenWrite(filePath))
            {
                using(System.IO.StreamWriter sw = new System.IO.StreamWriter(stream))
                {
                    foreach(var kv in data)
                    {
                        sw.WriteLine(string.Format("{0}{2}{1}", kv.Key, kv.Value, SEPARATOR));
                    }
                }
            }
        }
    }
}
