using System.Collections;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Camera cam;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 10f;
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadDuration = 0.2f;
    [SerializeField] private SessionManager sessionManager;

    [Header("Gun Recoil Object")]
    [SerializeField] private GunRecoil gunRecoil;

    [Header("View Recoil")]
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private float viewRecoilUp = 2f;
    [SerializeField] private float viewRecoilSide = 1f;

    [Header("Visual Effects")]
    [SerializeField] private Transform muzzleTransform;
    [SerializeField] private LineRenderer tracerLine;
    [SerializeField] private float tracerDuration = 0.04f;
    [SerializeField] private GameObject hitImpactPrefab;
    [SerializeField] private float hitImpactLifetime = 4f;

    private int bulletsInMag;
    private float nextFireTime;
    private bool isReloading;

    void Start()
    {
        ResetMagazine();

        if (tracerLine != null)
            tracerLine.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            StartReload();

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
            TryFire();
    }

    public void ResetMagazine()
    {
        bulletsInMag = magazineSize;

        if (sessionManager != null)
            sessionManager.UpdateAmmoDisplay(bulletsInMag, magazineSize, false);
    }

    private void TryFire()
    {
        if (isReloading)
            return;

        if (bulletsInMag <= 0)
        {
            if (sessionManager != null)
                sessionManager.UpdateAmmoDisplay(bulletsInMag, magazineSize, true);

            return;
        }

        nextFireTime = Time.time + 1f / fireRate;
        bulletsInMag--;

        if (sessionManager != null && cam != null)
        {
            sessionManager.RegisterShot(cam.transform.position, cam.transform.forward);
            sessionManager.UpdateAmmoDisplay(bulletsInMag, magazineSize, bulletsInMag == 0);
        }

        FireRay();

        if (gunRecoil != null)
            gunRecoil.ApplyRecoil();

        if (playerLook != null)
        {
            float sideKick = Random.Range(-viewRecoilSide, viewRecoilSide);
            playerLook.AddRecoil(viewRecoilUp, sideKick);
        }
    }

    private void FireRay()
    {
        if (cam == null)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, range);

        Vector3 tracerStart = muzzleTransform != null ? muzzleTransform.position : ray.origin;
        Vector3 tracerEnd = hitSomething ? hit.point : ray.origin + ray.direction * range;

        if (tracerLine != null)
            StartCoroutine(ShowTracer(tracerStart, tracerEnd));

        if (!hitSomething)
        {
            if (sessionManager != null)
                sessionManager.RegisterRecoilMiss();

            return;
        }

        if (hitImpactPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(hit.normal);
            GameObject impact = Object.Instantiate(hitImpactPrefab, hit.point, rot);
            Object.Destroy(impact, hitImpactLifetime);
        }

        Wave2Target wave2Target = hit.collider.GetComponentInParent<Wave2Target>();
        Target flickTarget = hit.collider.GetComponentInParent<Target>();

        if (sessionManager != null)
        {
            if (wave2Target != null)
            {
                sessionManager.RegisterHit(true);
                sessionManager.RegisterRecoilSample(hit.point, wave2Target.GetCenterWorldPosition(), wave2Target.GetInsideRadius());
            }
            else if (flickTarget != null)
            {
                sessionManager.RegisterHit(true);
            }
            else
            {
                sessionManager.RegisterHit(false);
                sessionManager.RegisterRecoilMiss();
            }
        }

        if (flickTarget != null)
            flickTarget.OnHit();

        if (wave2Target != null)
            wave2Target.OnHit();
    }

    private void StartReload()
    {
        if (isReloading)
            return;

        if (bulletsInMag == magazineSize)
            return;

        if (sessionManager != null)
            sessionManager.RegisterReloadStarted();

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadDuration);
        bulletsInMag = magazineSize;
        isReloading = false;

        if (sessionManager != null)
        {
            sessionManager.RegisterReloadFinished();
            sessionManager.UpdateAmmoDisplay(bulletsInMag, magazineSize, false);
        }
    }

    private IEnumerator ShowTracer(Vector3 start, Vector3 end)
    {
        if (tracerLine == null)
            yield break;

        tracerLine.positionCount = 2;
        tracerLine.SetPosition(0, start);
        tracerLine.SetPosition(1, end);
        tracerLine.enabled = true;

        yield return new WaitForSeconds(tracerDuration);

        tracerLine.enabled = false;
    }
}
