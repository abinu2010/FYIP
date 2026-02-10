using UnityEngine;

public class Wave : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] public float wave1Duration = 30f;

    [Header("Targets")]
    [SerializeField] private GameObject flickTarget;
    [SerializeField] private Transform[] wave2Targets;

    [Header("Wave 2 Hits")]
    [SerializeField] private int wave2HitsToDestroyPerTarget = 30;
    [SerializeField] private float wave2MinSpacing = 3f;

    [Header("Spawn Areas")]
    [SerializeField] private Vector3 wave1AreaCenter = new Vector3(0f, 1.5f, 15f);
    [SerializeField] private Vector3 wave1AreaSize = new Vector3(8f, 3f, 2f);
    [SerializeField] private Vector3 wave2AreaCenter = new Vector3(0f, 1.5f, 15f);
    [SerializeField] private Vector3 wave2AreaSize = new Vector3(8f, 3f, 2f);

    [Header("References")]
    [SerializeField] private SessionManager sessionManager;
    [SerializeField] private PlayerShoot playerShoot;

    private bool sequenceActive = false;
    private bool wave2Active = false;
    private float sequenceStartTime = 0f;
    private int wave2TargetsDestroyed = 0;
    private int activeWave2TargetsCount = 0; // NEW: To track currently active targets
    private Wave2Target[] wave2TargetScripts;

    void Awake()
    {
        if (wave2Targets != null && wave2Targets.Length > 0)
        {
            wave2TargetScripts = new Wave2Target[wave2Targets.Length];

            for (int i = 0; i < wave2Targets.Length; i++)
            {
                Transform t = wave2Targets[i];
                if (t == null)
                    continue;

                Wave2Target w2 = t.GetComponent<Wave2Target>();
                if (w2 == null)
                {
                    w2 = t.gameObject.AddComponent<Wave2Target>();
                }

                w2.Setup(this, wave2HitsToDestroyPerTarget);
                wave2TargetScripts[i] = w2;
            }
        }
    }

    void Update()
    {
        if (!sequenceActive)
            return;

        float elapsed = Time.time - sequenceStartTime;

        if (!wave2Active && elapsed >= wave1Duration)
        {
            StartWaveTwo();
        }
    }

    public void BeginWaveSequence()
    {
        sequenceActive = true;
        wave2Active = false;
        sequenceStartTime = Time.time;
        wave2TargetsDestroyed = 0;

        Debug.Log("WaveController: Sequence started");

        if (flickTarget != null)
        {
            flickTarget.SetActive(true);
            RespawnWave1Target();
        }

        if (wave2Targets != null)
        {
            foreach (Transform t in wave2Targets)
            {
                if (t != null)
                    t.gameObject.SetActive(false);
            }
        }

        if (sessionManager != null)
            sessionManager.SetWave(1);
    }

    public void StopWaveSequence()
    {
        sequenceActive = false;
        wave2Active = false;

        Debug.Log("WaveController: Sequence stopped");

        if (flickTarget != null)
            flickTarget.SetActive(false);

        if (wave2Targets != null)
        {
            foreach (Transform t in wave2Targets)
            {
                if (t != null)
                    t.gameObject.SetActive(false);
            }
        }
    }

    private void StartWaveTwo()
    {
        wave2Active = true;
        // wave2TargetsDestroyed = 0; // REMOVED: This is not for active target count

        Debug.Log("WaveController: Wave two started");

        if (flickTarget != null)
            flickTarget.SetActive(false);

        activeWave2TargetsCount = 0; // Initialize active targets count
        SpawnWave2TargetsOnce(); // This method will now increment activeWave2TargetsCount

        if (playerShoot != null)
        {
            playerShoot.ResetMagazine();
            Debug.Log("WaveController: Wave two auto reload");
        }

        if (sessionManager != null)
            sessionManager.SetWave(2);
    }

    private void RespawnWave1Target()
    {
        if (flickTarget == null)
            return;

        Vector3 offset = new Vector3(
            Random.Range(-wave1AreaSize.x * 0.5f, wave1AreaSize.x * 0.5f),
            Random.Range(-wave1AreaSize.y * 0.5f, wave1AreaSize.y * 0.5f),
            Random.Range(-wave1AreaSize.z * 0.5f, wave1AreaSize.z * 0.5f)
        );

        flickTarget.transform.position = wave1AreaCenter + offset;
        Debug.Log("WaveController: Flick target at " + flickTarget.transform.position);
    }

    private Vector3 GetRandomWave2Position()
    {
        Vector3 offset = new Vector3(
            Random.Range(-wave2AreaSize.x * 0.5f, wave2AreaSize.x * 0.5f),
            Random.Range(-wave2AreaSize.y * 0.5f, wave2AreaSize.y * 0.5f),
            Random.Range(-wave2AreaSize.z * 0.5f, wave2AreaSize.z * 0.5f)
        );

        return wave2AreaCenter + offset;
    }

    private void SpawnWave2TargetsOnce()
    {
        if (wave2Targets == null || wave2Targets.Length == 0)
            return;

        activeWave2TargetsCount = 0; // Reset count before spawning

        Transform firstTarget = wave2Targets[0];
        if (firstTarget != null)
        {
            Vector3 firstPos = GetRandomWave2Position();
            firstTarget.position = firstPos;
            firstTarget.gameObject.SetActive(true);
            activeWave2TargetsCount++; // Increment count for first target

            if (wave2TargetScripts != null && wave2TargetScripts.Length > 0 && wave2TargetScripts[0] != null)
            {
                wave2TargetScripts[0].ResetHits();
            }
        }

        if (wave2Targets.Length > 1)
        {
            Transform secondTarget = wave2Targets[1];
            if (secondTarget != null)
            {
                float minSqrDistance = wave2MinSpacing * wave2MinSpacing;
                Vector3 firstPos = wave2Targets[0].position;
                Vector3 chosenPos = firstPos;
                bool found = false;

                for (int attempt = 0; attempt < 20; attempt++)
                {
                    Vector3 candidate = GetRandomWave2Position();
                    if ((candidate - firstPos).sqrMagnitude >= minSqrDistance)
                    {
                        chosenPos = candidate;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    chosenPos = firstPos + new Vector3(wave2MinSpacing, 0f, 0f);
                }

                secondTarget.position = chosenPos;
                secondTarget.gameObject.SetActive(true);
                activeWave2TargetsCount++; // Increment count for second target

                if (wave2TargetScripts != null && wave2TargetScripts.Length > 1 && wave2TargetScripts[1] != null)
                {
                    wave2TargetScripts[1].ResetHits();
                }
            }
        }

        Debug.Log("WaveController: Wave two targets spawned with spacing. Active targets: " + activeWave2TargetsCount);
    }

    public void OnWave2TargetDestroyed(Wave2Target target)
    {
        if (!wave2Active)
            return;

        // wave2TargetsDestroyed++; // This variable is no longer used for this logic.
        activeWave2TargetsCount--; // Decrement the count of active targets

        Debug.Log("WaveController: Wave two target destroyed. Active targets remaining: " + activeWave2TargetsCount);

        // Check if all active targets have been destroyed
        if (activeWave2TargetsCount <= 0) // Changed condition
        {
            wave2Active = false;

            if (sessionManager != null)
            {
                sessionManager.RequestSessionEndFromWave();
            }
        }
    }

}
