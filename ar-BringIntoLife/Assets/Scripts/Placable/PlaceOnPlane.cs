using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Listens for touch events and performs an AR raycast from the screen touch point.
/// AR raycasts will only hit detected trackables like feature points and planes.
///
/// If a raycast hits a trackable, the <see cref="placedPrefab"/> is instantiated
/// and moved to the hit position.
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class PlaceOnPlane : MonoBehaviour
{
    ARRaycastManager m_RaycastManager;
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    [SerializeField]
    [Tooltip("Instantiates this prefab on a plane at the touch location.")]
    GameObject m_PlacedPrefab;

    [SerializeField]
    private int maxPrefabSpawnCount = 0;
    private int placedPrefabCount;
    
    [SerializeField]
    private GameObject UIPanel;

    /// <summary>
    /// The prefab to instantiate on touch.
    /// </summary>
    public GameObject placedPrefab
    {
        get { return m_PlacedPrefab; }
        set { m_PlacedPrefab = value; }
    }

    /// <summary>
    /// The object instantiated as a result of a successful raycast intersection with a plane.
    /// </summary>
    public GameObject spawnedObject { get; private set; }
    private List<GameObject> placedPrefabList = new List<GameObject>();

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if (Input.GetTouch(0).phase == TouchPhase.Began)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }
        touchPosition = default;
        return false;
    }

    void Update()
    {
        if (!TryGetTouchPosition(out Vector2 touchPosition))
            return;

        bool isOverUI = touchPosition.IsPointOverUIObject();

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            // Raycast hits are sorted by distance, so the first one
            // will be the closest hit.
            var hitPose = s_Hits[0].pose;

            // Let the placedObject face the camera
            var cameraForward = Camera.main.transform.forward;
            var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            hitPose.rotation = Quaternion.LookRotation(cameraBearing);

            if (!isOverUI && placedPrefabCount < maxPrefabSpawnCount)
            {
                SpawnPrefab(hitPose);
            }
        }
    }

    private void SpawnPrefab(Pose hitPose)
    {
        spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position, hitPose.rotation);
        placedPrefabList.Add(spawnedObject);
        placedPrefabCount++;
    }

    public void Clear()
    {
        foreach(GameObject spawnedObject in placedPrefabList)
        {
            GameObject.Destroy(spawnedObject);
        }
        placedPrefabCount = 0;
    }

}
