using UnityEngine;
using System.Collections;

public class TargetResetSystem : MonoBehaviour
{
    [Header("Motion configuration")]
    [Tooltip("O ile jednostek i w którą stronę ma się przesunąć. Np. Z=5 oddali o 5 metrów.")]
    public Vector3 moveOffset = new Vector3(0, 0, 5f);
    public float moveSpeed = 2.0f;
    public bool CleanUpInNextToogle = true;

    [Header("Cleanup")]
    [Tooltip("Przypisz tutaj obiekt (dziecko), którego NIE wolno usuwać (np. model tarczy). Wszystko inne (dziury) zostanie usunięte.")]
    public GameObject objectToKeep;

    // Stan prywatny
    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isMovedAway = false;
    private Coroutine currentMoveCoroutine;

    void Start()
    {
        // Zapamiętujemy pozycję startową (lokalną, żeby działało nawet jak przesuniesz całą strzelnicę)
        startPos = transform.localPosition;
        targetPos = startPos + moveOffset;
    }

    //Tę funkcję podepnij pod przycisk lub wywołaj z innego skryptu
    public void TogglePositionAndClean()
    {
        // 1. Najpierw sprzątamy śmieci (dziury po kulach)
        CleanUpChildren();

        // 2. Ustalamy cel podróży (Toggle)
        Vector3 destination = isMovedAway ? startPos : targetPos;
        isMovedAway = !isMovedAway;

        // 3. Uruchamiamy płynny ruch
        if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);
        currentMoveCoroutine = StartCoroutine(MoveRoutine(destination));
    }

    private void CleanUpChildren()
    {
        if (!CleanUpInNextToogle) {
            CleanUpInNextToogle = true;
            return;
        } 
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            // Jeśli to jest nasza tarcza (objectToKeep), to ją zostawiamy.
            // Jeśli objectToKeep nie jest przypisany, to dla bezpieczeństwa też nic nie usuwamy (chyba że chcesz wyczyścić wszystko).
            if (objectToKeep != null && child.gameObject == objectToKeep)
            {
                continue; // Pomiń ten krok, nie niszcz tego
            }

            // Wszystko inne (np. dziury po kulach, decale) niszczymy
            Destroy(child.gameObject);
        }
        CleanUpInNextToogle = false;
    }

    private IEnumerator MoveRoutine(Vector3 destination)
    {
        while (Vector3.Distance(transform.localPosition, destination) > 0.01f)
        {
            // Płynne przesunięcie w kierunku celu
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Dociągnięcie do idealnej pozycji na koniec
        transform.localPosition = destination;
    }
}