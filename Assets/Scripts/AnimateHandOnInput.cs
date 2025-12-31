using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit; // 🔹 Dodano: Potrzebne do wykrywania chwytania
// using UnityEngine.XR.Interaction.Toolkit.Interactors; // 🔹 Odkomentuj tę linię, jeśli używasz Unity 6 / XR Toolkit 3.0+

public class AnimateHandOnInput : MonoBehaviour
{
    [Header("Input")]
    public InputActionProperty triggerValue;
    public InputActionProperty gripValue;

    [Header("Visuals")]
    public Animator handAnimator;

    [Tooltip("Przypisz tutaj Skinned Mesh Renderer swojej dłoni (to, co ma znikać).")]
    public SkinnedMeshRenderer handMesh;

    [Tooltip("Jeśli puste, skrypt sam poszuka Interactora w rodzicach.")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 1. Automatyczne szukanie kontrolera (Interactora) w rodzicach, jeśli nie przypisałeś ręcznie
        if (interactor == null)
        {
            interactor = GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
        }

        // 2. Podpięcie się pod zdarzenia chwytania
        if (interactor != null)
        {
            interactor.selectEntered.AddListener(OnGrab);
            interactor.selectExited.AddListener(OnRelease);
        }
        else
        {
            Debug.LogWarning("AnimateHandOnInput: Nie znaleziono XR Interactor! Ręka nie będzie znikać.", this);
        }

        // 🔹 3. Automatyczne znalezienie Mesha, jeśli zapomniałeś przypisać
        if (handMesh == null && handAnimator != null)
        {
            handMesh = handAnimator.GetComponentInChildren<SkinnedMeshRenderer>();
        }
    }

    void OnDestroy()
    {
        // Sprzątanie eventów, żeby nie było błędów przy zmianie scen
        if (interactor != null)
        {
            interactor.selectEntered.RemoveListener(OnGrab);
            interactor.selectExited.RemoveListener(OnRelease);
        }
    }

    // Update is called once per frame
    void Update()
    {
        float trigger = triggerValue.action.ReadValue<float>();
        float grip = gripValue.action.ReadValue<float>();

        handAnimator.SetFloat("Trigger", trigger);
        handAnimator.SetFloat("Grip", grip);
    }

    // --- 🔹 NOWE FUNKCJE ---

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Opcjonalnie: Możesz tu dodać if, żeby znikać tylko przy broniach, np.:
        // if (args.interactableObject.transform.CompareTag("Weapon"))

        if (handMesh != null)
        {
            handMesh.enabled = false; // Wyłączamy widoczność, ale skrypt dalej działa
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (handMesh != null)
        {
            handMesh.enabled = true; // Przywracamy widoczność
        }
    }
}