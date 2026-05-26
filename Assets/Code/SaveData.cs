using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    [Header("Yarn / Діалог")]
    public string yarnNodeName = "";// вузол, з якого відновлюємо

    [Header("Статистика")]
    public int fear = 0;
    public int trustSoleim = 50;
    public int currentBPM = 72;

    [Header("Поточний стан сцени")]
    public string currentBackground = "";// останній bg з <<bg>>
    public string currentMusic = "";// останній трек з <<music>>

    [Header("Мета")]
    public string savedAt = "";// дата/час збереження
    public bool hasSave = false;// є активне збереження?
}
