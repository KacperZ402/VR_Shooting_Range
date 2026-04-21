using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponSpawnManager : MonoBehaviour
{
    public WeaponData[] weapons;
    public GameObject buttonPrefab;
    public Transform gridContainer;
    
    [Header("Spawn Points")]
    public Transform weaponSpawnPoint;
    public Transform magazineSpawnPoint;

    private Dictionary<string, Queue<GameObject>> weaponPools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, Queue<GameObject>> magazinePools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<GameObject, WeaponData> weaponDataMap = new Dictionary<GameObject, WeaponData>();
    private Dictionary<GameObject, WeaponData> magazineDataMap = new Dictionary<GameObject, WeaponData>();
    
    private GameObject currentActiveWeapon;
    private List<GameObject> currentActiveMagazines = new List<GameObject>();
    private WeaponData currentSelectedWeapon;

    void Start()
    {
        InitializePools();
        GenerateMenu();
    }

    void InitializePools()
    {
        foreach (var data in weapons)
        {
            // Pula broni
            Queue<GameObject> wPool = new Queue<GameObject>();
            for (int i = 0; i < data.poolSize; i++)
            {
                GameObject obj = Instantiate(data.weaponPrefab);
                obj.SetActive(false);
                wPool.Enqueue(obj);
                weaponDataMap[obj] = data;
            }
            weaponPools.Add(data.weaponName, wPool);

            // Pula magazynków
            if (data.magazinePrefab != null)
            {
                Queue<GameObject> mPool = new Queue<GameObject>();
                for (int i = 0; i < data.poolSize * data.magsToSpawn; i++)
                {
                    GameObject mag = Instantiate(data.magazinePrefab);
                    mag.SetActive(false);
                    mPool.Enqueue(mag);
                    magazineDataMap[mag] = data;
                }
                magazinePools.Add(data.weaponName + "_Mag", mPool);
            }
        }
    }

    public void RequestWeapon(WeaponData data)
    {
        if (currentSelectedWeapon == data)
            return;

        currentSelectedWeapon = data;
        CleanupCurrentItems();

        // Spawn Broni
        if (weaponPools.ContainsKey(data.weaponName))
        {
            currentActiveWeapon = weaponPools[data.weaponName].Dequeue();
            PrepareObject(currentActiveWeapon, weaponSpawnPoint);
        }

        // Spawn Magazynków
        string magKey = data.weaponName + "_Mag";
        if (magazinePools.ContainsKey(magKey))
        {
            for (int i = 0; i < data.magsToSpawn; i++)
            {
                if (magazinePools[magKey].Count == 0)
                    break;
                
                GameObject mag = magazinePools[magKey].Dequeue();
                Vector3 offset = new Vector3(i * 0.1f, 0, 0); 
                PrepareObject(mag, magazineSpawnPoint, offset);
                currentActiveMagazines.Add(mag);
            }
        }
    }

    void PrepareObject(GameObject obj, Transform sp, Vector3 offset = default)
    {
        obj.transform.position = sp.position + offset;
        obj.transform.rotation = sp.rotation;
        obj.SetActive(true);

        if (obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void CleanupCurrentItems()
    {
        // Broń
        if (currentActiveWeapon != null)
        {
            WeaponData weaponData = GetWeaponData(currentActiveWeapon, weaponDataMap);
            if (weaponData != null)
            {
                currentActiveWeapon.SetActive(false);
                weaponPools[weaponData.weaponName].Enqueue(currentActiveWeapon);
            }
        }

        // Magazynki
        if (currentActiveMagazines.Count > 0)
        {
            foreach (GameObject mag in currentActiveMagazines)
            {
                mag.SetActive(false);
                
                WeaponData magData = GetWeaponData(mag, magazineDataMap);
                if (magData != null)
                {
                    string magKey = magData.weaponName + "_Mag";
                    if (magazinePools.ContainsKey(magKey))
                    {
                        magazinePools[magKey].Enqueue(mag);
                    }
                    else
                    {
                        Destroy(mag);
                    }
                }
                else
                {
                    Destroy(mag);
                }
            }
        }

        currentActiveWeapon = null;
        currentActiveMagazines.Clear();
    }

    private WeaponData GetWeaponData(GameObject obj, Dictionary<GameObject, WeaponData> dataMap)
    {
        if (dataMap.ContainsKey(obj))
            return dataMap[obj];
        
        return null;
    }

    void GenerateMenu()
    {
        foreach (var data in weapons)
        {
            GameObject btn = Instantiate(buttonPrefab, gridContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = data.weaponName;
            btn.GetComponent<Button>().onClick.AddListener(() => RequestWeapon(data));
        }
    }
}