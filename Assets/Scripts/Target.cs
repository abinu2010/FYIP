using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField] private Vector3 spawnAreaCenter = new Vector3(0f, 1.5f, 15f);
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(8f, 3f, 2f);
    [SerializeField] private Transform centerPoint;
    [SerializeField] private SessionManager sessionManager;

    private Renderer cachedRenderer;

    void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer == null)
            cachedRenderer = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        Respawn();
    }

    public Vector3 GetCenterWorldPosition()
    {
        if (centerPoint != null)
            return centerPoint.position;

        Collider col = GetComponent<Collider>();
        if (col != null)
            return col.bounds.center;

        col = GetComponentInChildren<Collider>();
        if (col != null)
            return col.bounds.center;

        return transform.position;
    }

    public void OnHit()
    {
        if (cachedRenderer != null)
            StartCoroutine(FlashColor());

        Respawn();
    }

    private void Respawn()
    {
        Vector3 offset = new Vector3(
            Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
            Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f),
            Random.Range(-spawnAreaSize.z * 0.5f, spawnAreaSize.z * 0.5f)
        );

        transform.position = spawnAreaCenter + offset;

        if (sessionManager != null)
            sessionManager.RegisterFlickTargetSpawn(GetCenterWorldPosition());
    }

    private System.Collections.IEnumerator FlashColor()
    {
        Color original = cachedRenderer.material.color;
        cachedRenderer.material.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        cachedRenderer.material.color = original;
    }
}
