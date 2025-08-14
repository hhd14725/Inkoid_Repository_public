using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Bullet_Base))]
public class CustomProjectileEffects : MonoBehaviour
{
    [Header("Assign Prefabs (Effects)")]
    public GameObject muzzleEffectPrefab;
    public GameObject trailEffectPrefab;
    public GameObject impactEffectPrefab;
    [Tooltip("Offset along collision normal to reduce clipping")]
    public float impactOffset = 0.1f;

    private GameObject trailInstance;
    private ParticleSystem[] trailSystems;
    private Rigidbody rb;

    [Header("Assign Clips (SFX)")]
    public AudioClip muzzleSfx;
    public AudioClip impactSfx;
    [Header("Per-Effect Volume Scale")]
    [Range(0f, 2f)] public float muzzleVolumeScale = 0.5f;
    [Range(0f, 2f)] public float impactVolumeScale = 1.2f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Cache child trail particle systems for detachment
        trailSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    void OnEnable()
    {
        // 1) Muzzle flash effect: preserve prefab's own scale
        if (muzzleEffectPrefab != null)
        {
            var muzzle = Instantiate(
                muzzleEffectPrefab,
                transform.position,
                transform.rotation);
            SoundManager.Instance.PlaySfx(muzzleSfx, muzzleVolumeScale);
            Destroy(muzzle, 1.5f);

        }
        // 2) Trail effect attached as child
        if (trailEffectPrefab != null)
        {
            if (trailInstance != null)
                Destroy(trailInstance);

            trailInstance = Instantiate(
                trailEffectPrefab,
                transform.position,
                transform.rotation,
                transform);

            var ps = trailInstance.GetComponent<ParticleSystem>();
            if (ps != null)
                ps.Play();
        }
    }

    void Update()
    {
        // Orient projectile to velocity for visual consistency
        if (rb != null && rb.velocity.sqrMagnitude > 0f)
            transform.rotation = Quaternion.LookRotation(rb.velocity);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Impact visual effect: preserve prefab's scale
        if (impactEffectPrefab != null)
        {
            var contact = collision.contacts[0];
            Vector3 pos = contact.point + contact.normal * impactOffset;
            var impact = Instantiate(
                impactEffectPrefab,
                pos,
                Quaternion.FromToRotation(Vector3.up, contact.normal));
            SoundManager.Instance.PlaySfx(impactSfx, impactVolumeScale);
            Destroy(impact, 3.5f);
        }

        // Detach cached trail particle systems
        foreach (var ps in trailSystems)
        {
            if (ps.gameObject.name.Contains("Trail"))
            {
                ps.transform.SetParent(null);
                Destroy(ps.gameObject, 2f);
            }
        }
        // Detach instantiated trail prefab instance
        if (trailInstance != null)
        {
            trailInstance.transform.SetParent(null);
            Destroy(trailInstance, 2f);
            trailInstance = null;
        }

        // Let Bullet_Base.OnCollisionEnter handle paint and pooling
    }

    void OnDisable()
    {
        // No-op: trails handled on collision
    }
}
