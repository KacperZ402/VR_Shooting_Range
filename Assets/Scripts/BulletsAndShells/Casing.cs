using UnityEngine;

/// <summary>
/// Skrypt dla prefabu ³uski. Automatycznie wraca do puli po czasie.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Casing : MonoBehaviour
{
    public float lifetime = 5.0f;
    private float lifeTimer;
    private CasingPoolManager poolManager;

    void OnEnable()
    {
        if (poolManager == null)
            poolManager = CasingPoolManager.Instance;

        lifeTimer = lifetime;
    }

    void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            if (poolManager != null)
                poolManager.ReturnCasing(gameObject);
            else
                Destroy(gameObject); // Fallback
        }
    }
}