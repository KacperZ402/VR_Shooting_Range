using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Casing : MonoBehaviour
{
    [Header("Konfiguracja Puli")]
    public string poolTag = "Casing"; // Upewnij si�, �e tag zgadza si� z ObjectPooler
    public float lifeTime = 4.0f;

    [Header("Si�a Wyrzutu")]
    public Vector3 ejectionForce = new Vector3(1.5f, 0.5f, 0); // Si�a w lokalnej przestrzeni (prawo i g�ra)
    public float torqueAmount = 5.0f; // Losowa rotacja

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // OnEnable jest wywo�ywane, gdy SpawnFromPool ustawia SetActive(true)
    void OnEnable()
    {
        // Reset stanu fizyki
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Nadanie si�y i rotacji w lokalnych koordynatach wyrzutnika
        rb.AddRelativeForce(ejectionForce, ForceMode.Impulse);
        rb.AddRelativeTorque(Random.insideUnitSphere * torqueAmount, ForceMode.Impulse);

        // Zaplanuj powr�t do puli
        Invoke(nameof(Deactivate), lifeTime);
    }

    void OnDisable()
    {
        // Anuluj invoke, na wypadek gdyby obiekt zosta� wy��czony z innego powodu
        CancelInvoke(nameof(Deactivate));
    }

    private void Deactivate()
    {
        ObjectPooler.Instance.ReturnToPool(poolTag, this.gameObject);
    }
}