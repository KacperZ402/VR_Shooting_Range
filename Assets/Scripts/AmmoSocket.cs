using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSocketInteractor))]
public class AmmoSocket : MonoBehaviour
{
    [HideInInspector] public XRSocketInteractor socket;
    public Magazine currentMagazine;

    [Header("Filtr warstw")]
    public InteractionLayerMask allowedMagazineLayers;

    [Header("Eventy")]
    public UnityEvent OnMagazineInserted;
    public UnityEvent OnMagazineRemoved;

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        socket.selectEntered.AddListener(OnSelectEntered);
        socket.selectExited.AddListener(OnSelectExited);
    }

    void OnDestroy()
    {
        if (socket != null)
        {
            socket.selectEntered.RemoveListener(OnSelectEntered);
            socket.selectExited.RemoveListener(OnSelectExited);
        }
    }

    // Logika wpinania magazynka pozostaje bez zmian
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Sprawdzenie, czy warstwa magazynka jest dozwolona
        if ((allowedMagazineLayers.value & args.interactableObject.interactionLayers.value) == 0)
            return;

        var mag = args.interactableObject.transform.GetComponentInParent<Magazine>();
        if (mag != null)
        {

            currentMagazine = mag;
            currentMagazine.NotifyInserted();
            OnMagazineInserted?.Invoke();
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (currentMagazine != null)
        {
            currentMagazine.NotifyRemoved();
            OnMagazineRemoved?.Invoke();
            currentMagazine = null;
        }
    }

    // 🔹 ZMIANA: Zwraca prefab pocisku (GameObject) zamiast bool
    /// <summary>
    /// Próbuje pobrać prefab naboju z magazynka.
    /// </summary>
    /// <returns>Prefab pocisku (GameObject) lub null, jeśli się nie udało.</returns>
    public GameObject TryTakeRound()
    {
        if (currentMagazine == null) return null;
        return currentMagazine.ExtractRound();
    }

    // Bez zmian
    public int GetRoundsRemaining()
    {
        return currentMagazine != null ? currentMagazine.currentRounds : 0;
    }
}