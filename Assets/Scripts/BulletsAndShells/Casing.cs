using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Casing : MonoBehaviour
{
    public float lifetime = 5.0f;
    private float lifeTimer;
    private CasingPoolManager poolManager;

    //  NOWE: Zapamiêtywanie skali
    [HideInInspector] public Vector3 defaultScale;

    void Awake()
    {
        // Zapisz skalê prefabu
        defaultScale = transform.localScale;
    }

    void OnEnable()
    {
        if (poolManager == null) poolManager = CasingPoolManager.Instance;
        lifeTimer = lifetime;
    }

    // ... Update() bez zmian ...
    void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            if (poolManager != null) poolManager.ReturnCasing(gameObject);
            else Destroy(gameObject);
        }
    }
}