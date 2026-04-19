using UnityEngine;
public class MovingTarget : MonoBehaviour
{
    [Header("Motion conf")]
    [Tooltip("Jak szybko cel się porusza.")]
    public float speed = 3.0f;

    [Tooltip("Jak szeroko cel chodzi na boki (od punktu startu). Np. 3 oznacza ruch o 3 w lewo i 3 w prawo.")]
    public float range = 3.0f;

    [Tooltip("Czy cel ma się poruszać? Możesz to wyłączyć w inspektorze.")]
    public bool isMoving = true;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }
    void Update()
    {
        if (!isMoving) return;
        // Matematyka "Ping-Pong" - wartość rośnie i maleje liniowo
        // Time.time * speed -> napędza ruch
        // range * 2 -> określa pełną drogę (od lewej do prawej)
        // - range -> centruje ruch wokół punktu startowego
        float movement = Mathf.PingPong(Time.time * speed, range * 2) - range;
        // Aplikujemy ruch tylko na osi X (lewo/prawo), reszta bez zmian
        transform.position = new Vector3(startPosition.x + movement, transform.position.y, transform.position.z);
    }
    // 🔹 Rysuje pomocnicze linie w edytorze (żebyś widział trasę celu)
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) startPosition = transform.position;

        Gizmos.color = Color.green;
        Vector3 leftLimit = startPosition + Vector3.left * range;
        Vector3 rightLimit = startPosition + Vector3.right * range;

        Gizmos.DrawLine(leftLimit, rightLimit);
        Gizmos.DrawSphere(leftLimit, 0.1f);
        Gizmos.DrawSphere(rightLimit, 0.1f);
    }
}