using System.IO;
using System.Globalization;
using UnityEngine;
using TMPro;

public class SessionManager : MonoBehaviour
{
    [Header("Session Settings")]
    [SerializeField] private float sessionDuration = 90f;
    [SerializeField] private int maxRounds = 3;

    [Header("HUD UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text waveTitleText;
    [SerializeField] private TMP_Text waveInstructionText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text flickStatsText;
    [SerializeField] private TMP_Text recoilStatsText;

    [Header("End Summary UI")]
    [SerializeField] private TMP_Text summaryText;

    [Header("Start UI Root")]
    [SerializeField] private GameObject startUIRoot;

    [Header("Player Id Input")]
    [SerializeField] private TMP_InputField playerIdInput;

    [Header("Gameplay References")]
    [SerializeField] private PlayerShoot playerShoot;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private Wave waveController;

    [Header("CSV")]
    [SerializeField] private string csvFileName = "drill_results.csv";

    private bool sessionActive;
    private float timeRemaining;
    private float sessionStartTime;

    private int currentRound;
    private bool playerIdLocked;
    private string lockedPlayerId = "Player";

    private int totalShots;
    private int totalHits;

    private int currentWave;

    private int wave1Shots;
    private int wave1Hits;
    private float lastWave1HitTime = -1f;
    private float sumWave1HitIntervals;

    private int wave2Shots;
    private int wave2Hits;
    private int wave2Misses;
    private int wave2InsideHits;
    private int wave2OutsideHits;

    private int recoilSamples;
    private float sumRecoilError;

    private float sumAccuracyOverall;
    private float sumAccuracyWave1;
    private float sumAccuracyWave2;
    private float sumAvgKillTimeWave1;
    private float sumHitsPerMinute;
    private float sumAvgRecoilRadius;
    private float sumWave2Tightness;
    private float sumFlickScore;
    private float sumRecoilScore;
    private float sumFinalScore;

    void Start()
    {
        ResetAggregateStats();
        ResetToMenuState();
        Debug.Log("CSV path: " + Application.persistentDataPath);
    }

    void Update()
    {
        if (!sessionActive)
            return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndSession();
            return;
        }

        UpdateTimerUI();
    }

    public void StartSession()
    {
        LockPlayerIdIfNeeded();

        if (currentRound >= maxRounds)
        {
            ResetAggregateStats();
            currentRound = 0;
        }

        currentRound++;

        sessionActive = true;
        sessionStartTime = Time.time;
        timeRemaining = sessionDuration;

        totalShots = 0;
        totalHits = 0;

        currentWave = 1;

        wave1Shots = 0;
        wave1Hits = 0;
        lastWave1HitTime = -1f;
        sumWave1HitIntervals = 0f;

        wave2Shots = 0;
        wave2Hits = 0;
        wave2Misses = 0;
        wave2InsideHits = 0;
        wave2OutsideHits = 0;

        recoilSamples = 0;
        sumRecoilError = 0f;

        if (playerShoot != null)
        {
            playerShoot.enabled = true;
            playerShoot.ResetMagazine();
        }

        if (playerLook != null)
            playerLook.enabled = true;

        if (waveController != null)
            waveController.BeginWaveSequence();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetStartUIVisible(false);

        if (summaryText != null)
            summaryText.text = "";

        SetWave(1);
        UpdateTimerUI();
        UpdateLiveStats();
    }

    public void RequestSessionEndFromWave()
    {
        if (!sessionActive)
            return;

        EndSession();
    }

    private void EndSession()
    {
        if (!sessionActive)
            return;

        sessionActive = false;

        if (playerShoot != null)
            playerShoot.enabled = false;

        if (playerLook != null)
            playerLook.enabled = false;

        if (waveController != null)
            waveController.StopWaveSequence();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetStartUIVisible(true);

        float accuracyOverall = totalShots > 0 ? (totalHits / (float)totalShots) * 100f : 0f;
        float accuracyWave1 = wave1Shots > 0 ? (wave1Hits / (float)wave1Shots) * 100f : 0f;
        float accuracyWave2 = wave2Shots > 0 ? (wave2Hits / (float)wave2Shots) * 100f : 0f;

        float elapsed = Mathf.Max(0.01f, Time.time - sessionStartTime);
        float hitsPerMinute = totalHits > 0 ? (totalHits / elapsed) * 60f : 0f;

        float avgTimePerTargetWave1 = wave1Hits > 1 ? sumWave1HitIntervals / (wave1Hits - 1) : 0f;
        float avgRecoilRadius = recoilSamples > 0 ? sumRecoilError / recoilSamples : 0f;

        float tightness = recoilSamples > 0 ? (wave2InsideHits / (float)recoilSamples) * 100f : 0f;

        float flickScore = ComputeFlickScore(accuracyWave1, avgTimePerTargetWave1);
        float recoilScore = ComputeRecoilScore(accuracyWave2, tightness);
        float finalScore = (flickScore + recoilScore) * 0.5f;
        string rank = GetRank(finalScore);

        SaveSessionResult(
            accuracyOverall,
            accuracyWave1,
            accuracyWave2,
            avgTimePerTargetWave1,
            hitsPerMinute,
            avgRecoilRadius,
            tightness,
            flickScore,
            recoilScore,
            finalScore,
            currentRound
        );

        sumAccuracyOverall += accuracyOverall;
        sumAccuracyWave1 += accuracyWave1;
        sumAccuracyWave2 += accuracyWave2;
        sumAvgKillTimeWave1 += avgTimePerTargetWave1;
        sumHitsPerMinute += hitsPerMinute;
        sumAvgRecoilRadius += avgRecoilRadius;
        sumWave2Tightness += tightness;
        sumFlickScore += flickScore;
        sumRecoilScore += recoilScore;
        sumFinalScore += finalScore;

        if (summaryText != null)
        {
            if (currentRound < maxRounds)
            {
                summaryText.text =
                    "Player: " + lockedPlayerId + "\n" +
                    "Round " + currentRound + " of " + maxRounds + "\n" +
                    "Score: " + finalScore.ToString("F0") + " | " + rank + "\n" +
                    "Press Start for next round";
            }
            else
            {
                float avgOverallAccuracy = sumAccuracyOverall / maxRounds;
                float avgWave1Accuracy = sumAccuracyWave1 / maxRounds;
                float avgWave2Accuracy = sumAccuracyWave2 / maxRounds;
                float avgWave1Time = sumAvgKillTimeWave1 / maxRounds;
                float avgHpm = sumHitsPerMinute / maxRounds;
                float avgRecoil = sumAvgRecoilRadius / maxRounds;
                float avgTight = sumWave2Tightness / maxRounds;
                float avgFlick = sumFlickScore / maxRounds;
                float avgRecoilScore = sumRecoilScore / maxRounds;
                float avgFinal = sumFinalScore / maxRounds;
                string avgRank = GetRank(avgFinal);

                summaryText.text =
                    "Player: " + lockedPlayerId + "\n" +
                    "Rounds: " + maxRounds + "\n\n" +
                    "Avg overall accuracy: " + avgOverallAccuracy.ToString("F1") + "%\n" +
                    "Avg wave1 accuracy: " + avgWave1Accuracy.ToString("F1") + "%\n" +
                    "Avg wave2 accuracy: " + avgWave2Accuracy.ToString("F1") + "%\n" +
                    "Avg wave1 time/target: " + avgWave1Time.ToString("F2") + "s\n" +
                    "Avg hits per minute: " + avgHpm.ToString("F1") + "\n" +
                    "Avg recoil radius: " + avgRecoil.ToString("F2") + "\n" +
                    "Avg recoil tightness: " + avgTight.ToString("F1") + "%\n\n" +
                    "Avg flick score: " + avgFlick.ToString("F0") + "\n" +
                    "Avg recoil score: " + avgRecoilScore.ToString("F0") + "\n" +
                    "Avg drill score: " + avgFinal.ToString("F0") + "\n" +
                    "Skill level: " + avgRank;

                ResetAggregateStats();
                currentRound = 0;
            }
        }

        UpdateTimerUI();
        UpdateLiveStats();
        SetWave(0);
    }

    public void RegisterShot()
    {
        totalShots++;

        if (currentWave == 1)
            wave1Shots++;
        else if (currentWave == 2)
            wave2Shots++;

        UpdateLiveStats();
    }

    public void RegisterShot(Vector3 origin, Vector3 direction)
    {
        RegisterShot();
    }

    public void RegisterHit()
    {
        totalHits++;
        float now = Time.time;

        if (currentWave == 1)
        {
            if (lastWave1HitTime > 0f)
                sumWave1HitIntervals += now - lastWave1HitTime;

            lastWave1HitTime = now;
            wave1Hits++;
        }
        else if (currentWave == 2)
        {
            wave2Hits++;
        }

        UpdateLiveStats();
    }

    public void RegisterHit(bool isHit)
    {
        if (isHit)
            RegisterHit();
        else
            UpdateLiveStats();
    }

    public void RegisterRecoilMiss()
    {
        if (currentWave == 2)
            wave2Misses++;

        UpdateLiveStats();
    }

    public void RegisterFlickTargetSpawn(Vector3 targetCenterWorld)
    {
        if (!sessionActive)
            return;

        if (currentWave != 1)
            return;
    }

    public void RegisterRecoilSample(Vector3 hitPoint, Vector3 centerPoint)
    {
        RegisterRecoilSample(hitPoint, centerPoint, 0.15f);
    }

    public void RegisterRecoilSample(Vector3 hitPoint, Vector3 centerPoint, float insideRadius)
    {
        if (currentWave != 2)
            return;

        float error = Vector3.Distance(hitPoint, centerPoint);
        recoilSamples++;
        sumRecoilError += error;

        float r = insideRadius > 0f ? insideRadius : 0.15f;

        if (error <= r)
            wave2InsideHits++;
        else
            wave2OutsideHits++;

        UpdateLiveStats();
    }

    public void SetWave(int waveIndex)
    {
        currentWave = waveIndex;

        if (waveIndex == 1)
        {
            if (waveTitleText != null) waveTitleText.text = "Wave 1: Flick";
            if (waveInstructionText != null) waveInstructionText.text = "Hit the target, it respawns.";
            if (flickStatsText != null) flickStatsText.gameObject.SetActive(true);
            if (recoilStatsText != null) recoilStatsText.gameObject.SetActive(false);
        }
        else if (waveIndex == 2)
        {
            if (waveTitleText != null) waveTitleText.text = "Wave 2: Recoil";
            if (waveInstructionText != null) waveInstructionText.text = "Hold fire, keep shots tight.";
            if (flickStatsText != null) flickStatsText.gameObject.SetActive(false);
            if (recoilStatsText != null) recoilStatsText.gameObject.SetActive(true);
        }
        else
        {
            if (waveTitleText != null) waveTitleText.text = "";
            if (waveInstructionText != null) waveInstructionText.text = "";
            if (flickStatsText != null) flickStatsText.gameObject.SetActive(false);
            if (recoilStatsText != null) recoilStatsText.gameObject.SetActive(false);
        }

        UpdateLiveStats();
    }

    public void UpdateAmmoDisplay(int current, int max, bool showReloadHint)
    {
        if (ammoText == null)
            return;

        if (max <= 0)
        {
            ammoText.text = "Ammo: -";
            return;
        }

        if (showReloadHint && current == 0)
            ammoText.text = "Ammo: " + current + "/" + max + "  Press R";
        else
            ammoText.text = "Ammo: " + current + "/" + max;
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining).ToString() + "s";
    }

    private void UpdateLiveStats()
    {
        if (accuracyText != null)
        {
            float accuracy = totalShots > 0 ? (totalHits / (float)totalShots) * 100f : 0f;
            accuracyText.text = "Accuracy: " + accuracy.ToString("F1") + "%";
        }

        if (currentWave == 1 && flickStatsText != null)
        {
            if (wave1Hits > 1)
            {
                float avgTime = sumWave1HitIntervals / (wave1Hits - 1);
                flickStatsText.text = "Hits: " + wave1Hits + " | Avg time/target: " + avgTime.ToString("F2") + "s";
            }
            else
            {
                flickStatsText.text = "Hits: " + wave1Hits;
            }
        }

        if (currentWave == 2 && recoilStatsText != null)
        {
            float avgRadius = recoilSamples > 0 ? sumRecoilError / recoilSamples : 0f;
            float tight = recoilSamples > 0 ? (wave2InsideHits / (float)recoilSamples) * 100f : 0f;

            recoilStatsText.text =
                "Hits: " + wave2Hits +
                " | Miss: " + wave2Misses +
                " | In: " + wave2InsideHits +
                " | Out: " + wave2OutsideHits +
                " | Tight: " + tight.ToString("F1") + "% " +
                " | AvgR: " + (recoilSamples > 0 ? avgRadius.ToString("F2") : "-");
        }
    }

    private void LockPlayerIdIfNeeded()
    {
        if (playerIdLocked)
        {
            if (playerIdInput != null)
            {
                playerIdInput.gameObject.SetActive(false);
            }
            return;
        }

        string id = "Player";
        if (playerIdInput != null)
        {
            playerIdInput.gameObject.SetActive(true); 
            string input = playerIdInput.text.Trim();
            if (!string.IsNullOrEmpty(input))
                id = input;

            playerIdInput.text = id;
            playerIdInput.interactable = false;
            playerIdInput.readOnly = true;
        }

        lockedPlayerId = id;
        playerIdLocked = true;
    }

    private void SetStartUIVisible(bool visible)
    {
        if (startUIRoot != null)
            startUIRoot.SetActive(visible);
    }

    private void ResetToMenuState()
    {
        sessionActive = false;
        timeRemaining = sessionDuration;
        currentWave = 0;

        if (playerShoot != null) playerShoot.enabled = false;
        if (playerLook != null) playerLook.enabled = false;
        if (waveController != null) waveController.StopWaveSequence();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetStartUIVisible(true);
        SetWave(0);
        UpdateTimerUI();
        UpdateLiveStats();
        UpdateAmmoDisplay(0, 0, false);

        if (summaryText != null)
            summaryText.text = "Press Start to begin";
    }

    private void ResetAggregateStats()
    {
        sumAccuracyOverall = 0f;
        sumAccuracyWave1 = 0f;
        sumAccuracyWave2 = 0f;
        sumAvgKillTimeWave1 = 0f;
        sumHitsPerMinute = 0f;
        sumAvgRecoilRadius = 0f;
        sumWave2Tightness = 0f;
        sumFlickScore = 0f;
        sumRecoilScore = 0f;
        sumFinalScore = 0f;
    }

    private float ComputeFlickScore(float accuracyWave1, float avgTimePerTarget)
    {
        float timeScore = 0f;
        float tMax = 1.2f;
        if (avgTimePerTarget > 0f)
            timeScore = Mathf.Clamp01(1f - (avgTimePerTarget / tMax)) * 100f;

        return (accuracyWave1 * 0.6f) + (timeScore * 0.4f);
    }

    private float ComputeRecoilScore(float accuracyWave2, float tightness)
    {
        return (accuracyWave2 * 0.6f) + (tightness * 0.4f);
    }

    private string GetRank(float score)
    {
        if (score >= 90f) return "S";
        if (score >= 75f) return "A";
        if (score >= 60f) return "B";
        if (score >= 45f) return "C";
        return "D";
    }

    private void SaveSessionResult(
        float accuracyOverall,
        float accuracyWave1,
        float accuracyWave2,
        float avgTimePerTargetWave1,
        float hitsPerMinute,
        float avgRecoilRadius,
        float tightness,
        float flickScore,
        float recoilScore,
        float finalScore,
        int roundIndex
    )
    {
        string path = Path.Combine(Application.persistentDataPath, csvFileName);
        bool fileExists = File.Exists(path);

        try
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                if (!fileExists)
                    writer.WriteLine("Timestamp,PlayerId,Round,OverallAccuracy,Wave1Accuracy,Wave2Accuracy,Wave1AvgTimePerTarget,HitsPerMinute,Wave2AvgRecoilRadius,Wave2TightnessPercent,Wave2Misses,Wave2InsideHits,Wave2OutsideHits,FlickScore,RecoilScore,FinalScore,TotalShots,TotalHits,Wave1Shots,Wave1Hits,Wave2Shots,Wave2Hits");

                string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string safeId = lockedPlayerId.Replace(",", "_");

                string line = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3:F1},{4:F1},{5:F1},{6:F2},{7:F1},{8:F2},{9:F1},{10},{11},{12},{13:F0},{14:F0},{15:F0},{16},{17},{18},{19},{20},{21}",
                    timestamp,
                    safeId,
                    roundIndex,
                    accuracyOverall,
                    accuracyWave1,
                    accuracyWave2,
                    avgTimePerTargetWave1,
                    hitsPerMinute,
                    avgRecoilRadius,
                    tightness,
                    wave2Misses,
                    wave2InsideHits,
                    wave2OutsideHits,
                    flickScore,
                    recoilScore,
                    finalScore,
                    totalShots,
                    totalHits,
                    wave1Shots,
                    wave1Hits,
                    wave2Shots,
                    wave2Hits
                );

                writer.WriteLine(line);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("CSV save failed: " + ex.Message);
        }
    }
}
