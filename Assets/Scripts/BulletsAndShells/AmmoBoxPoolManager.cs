using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoBoxPoolManager : MonoBehaviour
{
    public static AmmoBoxPoolManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject[] ammoBoxPrefabs;

    [Header("UI")]
    public GameObject buttonPrefab;
    public Transform gridContainer;

    [Header("Spawn Point")]
    public Transform spawnPoint;

    private Dictionary<string, Queue<GameObject>> ammoBoxPools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<GameObject, string> boxTypeMap = new Dictionary<GameObject, string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializePools();
    }

    void Start()
    {
        GenerateMenu();
    }

    void InitializePools()
    {
        foreach (var prefab in ammoBoxPrefabs)
        {
            if (prefab == null) continue;

            string boxName = prefab.name;
            Queue<GameObject> pool = new Queue<GameObject>();

            for (int i = 0; i < 5; i++)
            {
                GameObject box = Instantiate(prefab);
                box.SetActive(false);
                pool.Enqueue(box);
                boxTypeMap[box] = boxName;
            }

            ammoBoxPools.Add(boxName, pool);
        }
    }

    public void RequestAmmoBox(string boxType)
    {
        if (!ammoBoxPools.ContainsKey(boxType))
            return;

        GameObject box;

        if (ammoBoxPools[boxType].Count > 0)
        {
            box = ammoBoxPools[boxType].Dequeue();
        }
        else
        {
            // Utwórz nową paczkę jeśli pula pusta
            var prefab = System.Array.Find(ammoBoxPrefabs, p => p != null && p.name == boxType);
            if (prefab == null) return;

            box = Instantiate(prefab);
            boxTypeMap[box] = boxType;
        }

        PrepareBox(box, boxType);
    }

    void PrepareBox(GameObject box, string boxType)
    {
        box.transform.position = spawnPoint.position;
        box.transform.rotation = spawnPoint.rotation;
        box.SetActive(true);

        var ammoBox = box.GetComponent<AmmoBox>();
        if (ammoBox != null)
            ammoBox.SetPoolManager(this);
    }

    public void ReturnAmmoBox(GameObject box)
    {
        box.SetActive(false);

        if (boxTypeMap.ContainsKey(box))
        {
            string boxType = boxTypeMap[box];
            if (ammoBoxPools.ContainsKey(boxType))
            {
                ammoBoxPools[boxType].Enqueue(box);
            }
        }
    }

    void GenerateMenu()
    {
        foreach (var prefab in ammoBoxPrefabs)
        {
            if (prefab == null) continue;

            GameObject btn = Instantiate(buttonPrefab, gridContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = prefab.name;

            string boxType = prefab.name;
            btn.GetComponent<Button>().onClick.AddListener(() => RequestAmmoBox(boxType));
        }
    }
}