using UnityEngine;

public class Wave2Target : MonoBehaviour
{
    [SerializeField] private int hitsToDestroy = 30;
    [SerializeField] private Transform centerPoint;
    [SerializeField] private float insideRadius = 0.15f;

    private int currentHits = 0;
    private Wave waveController;

    public void Setup(Wave controller, int hitsForDestroy)
    {
        waveController = controller;
        hitsToDestroy = hitsForDestroy;
        currentHits = 0;
    }

    public void ResetHits()
    {
        currentHits = 0;
    }

    public float GetInsideRadius()
    {
        return insideRadius;
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
        currentHits++;
        Debug.Log("Wave2Target: Hit " + currentHits + "/" + hitsToDestroy);

        if (currentHits >= hitsToDestroy)
        {
            gameObject.SetActive(false);

            if (waveController != null)
                waveController.OnWave2TargetDestroyed(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (centerPoint != null)
        {
            Gizmos.color = Color.cyan; // Choose a visible color
            Gizmos.DrawWireSphere(GetCenterWorldPosition(), insideRadius);
        }
    }
}
