using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodySocketInventory : MonoBehaviour
{
    public GameObject HMD;
    public GameObject[] bodySockets;
    public float socketYOffset = 0f;

    private Vector3 _currentHMDlocalPosition;
    private Quaternion _currentHMDRotation;
    private bool _useFirstSocket = true; // Przechowuj stan

    void Update()
    {
        _currentHMDlocalPosition = HMD.transform.localPosition;
        _currentHMDRotation = HMD.transform.rotation;

        foreach (var socket in bodySockets)
        {
            UpdateBodySocketHeight(socket);
        }

        UpdateSocketInventory();
    }

    private void UpdateBodySocketHeight(GameObject socket)
    {
        socket.transform.localPosition = new Vector3(
            socket.transform.localPosition.x,
            _currentHMDlocalPosition.y + socketYOffset,
            socket.transform.localPosition.z
        );
    }

    private void UpdateSocketInventory()
    {
        transform.localPosition = new Vector3(_currentHMDlocalPosition.x, 0, _currentHMDlocalPosition.z);
        transform.rotation = new Quaternion(transform.rotation.x, _currentHMDRotation.y, transform.rotation.z, _currentHMDRotation.w);
    }

    /// <summary>
    /// Toggle między socketami (bez argumentów)
    /// </summary>
    public void ToggleSocket()
    {
        _useFirstSocket = !_useFirstSocket;

        // Deaktywuj wszystkie sockety
        foreach (var socket in bodySockets)
        {
            socket.SetActive(false);
        }

        // Aktywuj wybrany
        int socketIndex = _useFirstSocket ? 0 : 1;
        if (socketIndex < bodySockets.Length)
        {
            bodySockets[socketIndex].SetActive(true);
            Debug.Log($"[BodySocketInventory] Aktywny socket: {bodySockets[socketIndex].name}");
        }
    }
}