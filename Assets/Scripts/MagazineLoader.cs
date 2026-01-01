using UnityEngine;

// Odkomentuj w Unity 6 / XR Toolkit 3.0+:
// using UnityEngine.XR.Interaction.Toolkit.Interactables; 

public class MagazineLoader : MonoBehaviour
{
    [SerializeField] private Magazine parentMagazine;

    private void Awake()
    {
        if (parentMagazine == null) parentMagazine = GetComponentInParent<Magazine>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Sprawdzamy, czy to nabój i czy magazynek ma miejsce
        if (!other.CompareTag("Bullet")) return;
        if (parentMagazine != null && parentMagazine.IsFull) return;

        // 2. Pobieramy komponent Interactable z naboju
        var bulletInteractable = other.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // 3. 🔥 WARUNEK ANULOWANIA GRABA 🔥
        // Sprawdzamy, czy nabój jest aktualnie trzymany przez rękę (isSelected)
        if (bulletInteractable != null && bulletInteractable.isSelected)
        {
            // Pobieramy menedżera interakcji
            var interactionManager = bulletInteractable.interactionManager;

            // JEŚLI RĘKA TRZYMA -> ZMUŚ JĄ DO PUSZCZENIA
            if (interactionManager != null)
            {
                // To jest ta linijka, która "anuluje garb"
                interactionManager.SelectExit(bulletInteractable.interactorsSelecting[0], bulletInteractable);
            }
        }

        // 4. Teraz, gdy nabój jest już wolny (lub zaraz będzie), wkładamy go do magazynka
        if (parentMagazine != null)
        {
            parentMagazine.TryInsertRound(other.gameObject);
        }
    }
}