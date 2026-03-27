using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager I { get; private set; }

    public const string SaveKey = "COSMIC_TERRARIUM_SAVE_V1";
    public const string TutorialSeenKey = "TUTORIAL_SEEN";

    [Header("Autosave")]
    [SerializeField] private float autosaveInterval = 5f;

    private float autosaveTimer;
    private bool hasFinishedStartupChoice;
    private Dictionary<string, PotStateData> loadedPotStates = new();

    public bool IsWaitingForResumeChoice { get; private set; }

    [Serializable]
    public class SaveData
    {
        public double seed;
        public double crystal;
        public double seedRate;
        public double crystalRate;
        public double lifetimeSeedProduced;
        public int plantedCount;
        public bool seedPortalUnlocked;
        public long lastSaveUnixSeconds;
        public List<PotStateData> pots = new();
    }

    [Serializable]
    public class PotStateData
    {
        public string potId;
        public bool planted;
        public int crystalsApplied;
    }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private IEnumerator Start()
    {
        while (ResourceManager.I == null)
            yield return null;

        Time.timeScale = 0f;

        if (HasSaveData())
        {
            IsWaitingForResumeChoice = true;

            ResumePromptUI prompt = FindFirstObjectByType<ResumePromptUI>(FindObjectsInactive.Include);
            if (prompt != null)
            {
                prompt.Show();
            }
            else
            {
                ResumeSavedGame();
            }
        }
        else
        {
            StartFreshGame();
        }
    }

    private void Update()
    {
        if (!hasFinishedStartupChoice || ResourceManager.I == null)
            return;

        autosaveTimer += Time.unscaledDeltaTime;
        if (autosaveTimer >= autosaveInterval)
        {
            autosaveTimer = 0f;
            SaveGame();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasFinishedStartupChoice)
            return;

        StartCoroutine(RestorePotsNextFrame());
    }

    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(SaveKey);
    }

    public void ResumeSavedGame()
    {
        IsWaitingForResumeChoice = false;
        LoadGame();
        hasFinishedStartupChoice = true;
        Time.timeScale = 1f;
    }

    public void StartFreshGame()
    {
        IsWaitingForResumeChoice = false;

        DeleteAllSaveData();

        if (ResourceManager.I != null)
        {
            ResourceManager.I.ApplyLoadedState(
                0d,
                0d,
                0d,
                0d,
                0d,
                0,
                false
            );
            ResourceManager.I.ClearPendingOfflineWelcome();
        }

        loadedPotStates.Clear();
        hasFinishedStartupChoice = true;
        Time.timeScale = 1f;

        StartCoroutine(RestorePotsNextFrame());
    }

    public void SaveGame()
    {
        if (!hasFinishedStartupChoice || ResourceManager.I == null)
            return;

        SaveData data = new SaveData
        {
            seed = ResourceManager.I.seed,
            crystal = ResourceManager.I.crystal,
            seedRate = ResourceManager.I.seedRate,
            crystalRate = ResourceManager.I.crystalRate,
            lifetimeSeedProduced = ResourceManager.I.lifetimeSeedProduced,
            plantedCount = ResourceManager.I.plantedCount,
            seedPortalUnlocked = ResourceManager.I.seedPortalUnlocked,
            lastSaveUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            pots = new List<PotStateData>()
        };

        PotPlanting[] pots = FindObjectsByType<PotPlanting>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PotPlanting pot in pots)
        {
            data.pots.Add(new PotStateData
            {
                potId = pot.PotId,
                planted = pot.IsPlanted,
                crystalsApplied = pot.CrystalsApplied
            });
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SaveKey) || ResourceManager.I == null)
        {
            loadedPotStates.Clear();
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrWhiteSpace(json))
            return;

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
            return;

        ResourceManager.I.ApplyLoadedState(
            data.seed,
            data.crystal,
            data.seedRate,
            data.crystalRate,
            data.lifetimeSeedProduced,
            data.plantedCount,
            data.seedPortalUnlocked
        );

        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsedSeconds = Mathf.Max(0, (int)(nowUnix - data.lastSaveUnixSeconds));

        if (elapsedSeconds > 0)
            ResourceManager.I.ApplyOfflineProgress(elapsedSeconds);

        loadedPotStates.Clear();
        if (data.pots != null)
        {
            foreach (PotStateData pot in data.pots)
            {
                loadedPotStates[pot.potId] = pot;
            }
        }

        StartCoroutine(RestorePotsNextFrame());
    }

    public void DeleteAllSaveData()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey(TutorialSeenKey);
        PlayerPrefs.Save();
    }

    private IEnumerator RestorePotsNextFrame()
    {
        yield return null;

        PotPlanting[] pots = FindObjectsByType<PotPlanting>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PotPlanting pot in pots)
        {
            if (loadedPotStates.TryGetValue(pot.PotId, out PotStateData state))
            {
                pot.RestoreFromSave(state.planted, state.crystalsApplied);
            }
            else
            {
                pot.RestoreFromSave(false, 0);
            }
        }
    }
}