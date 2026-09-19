using System;
using System.IO;
using UnityEngine;

public class FileDataHandler
{
    private string dataDirPath = "";
    private bool useEncryption = true;
    private readonly string encryptionCodeWord = "ChaveCriptografiaBolinhas123";

    public FileDataHandler(string dataDirPath, bool useEncryption)
    {
        this.dataDirPath = dataDirPath;
        this.useEncryption = useEncryption;
    }

    public SaveData Load(int slot)
    {
        string fullPath = Path.Combine(dataDirPath, $"save_slot_{slot}.json");
        SaveData loadedData = null;

        if (File.Exists(fullPath))
        {
            try
            {
                string dataToLoad = "";
                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                if (useEncryption)
                {
                    dataToLoad = EncryptDecrypt(dataToLoad);
                }

                loadedData = JsonUtility.FromJson<SaveData>(dataToLoad);
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar o slot {slot}: {e.Message}");
            }
        }
        return loadedData;
    }

    public void Save(SaveData data, int slot)
    {
        string fullPath = Path.Combine(dataDirPath, $"save_slot_{slot}.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            string dataToStore = JsonUtility.ToJson(data, true);

            if (useEncryption)
            {
                dataToStore = EncryptDecrypt(dataToStore);
            }

            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataToStore);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao salvar no slot {slot}: {e.Message}");
        }
    }

    public bool SlotExists(int slot)
    {
        string fullPath = Path.Combine(dataDirPath, $"save_slot_{slot}.json");
        return File.Exists(fullPath);
    }

    private string EncryptDecrypt(string data)
    {
        char[] result = new char[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (char)(data[i] ^ encryptionCodeWord[i % encryptionCodeWord.Length]);
        }
        return new string(result);
    }
}