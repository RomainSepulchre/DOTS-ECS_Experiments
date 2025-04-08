using Unity.Entities.UniversalDelegates;
using UnityEngine;

namespace Burst.SIMD.SimpleFustrum
{
    public class DrawCameraFustrumPlanes : MonoBehaviour
    {
        public Material[] materials;

        private Camera _camera;
        private Plane[] _cameraPlanes = new Plane[6];
        private Color[] _colors = new Color[]
        {
            new Color(1f,0.5f,0.5f),
            Color.red,
            new Color(0.5f,1f,0.5f),
            Color.green,
            new Color(0.5f,0.5f,1f),
            Color.blue,
        };
        private GameObject[] _planeObj = new GameObject[6];

        void Start()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            // Update _cameraPlanes
            GeometryUtility.CalculateFrustumPlanes(_camera, _cameraPlanes);

            // Go through all _cameraPlanes
            for (int i = 0; i < 6; i++)
            {
                CreatePlaneObject(i);
            }
        }

        void Update()
        {
            // Update _cameraPlanes
            GeometryUtility.CalculateFrustumPlanes(_camera, _cameraPlanes);

            // Go through all _cameraPlanes
            for (int i = 0; i < 6; i++)
            {
                DrawPlaneNormal(i);
                UpdatePlaneObject(i);
            }
        }

        private void DrawPlaneNormal(int i)
        {
            Debug.DrawLine(Vector3.zero, -(_cameraPlanes[i].normal * _cameraPlanes[i].distance), _colors[i]);
        }

        private void CreatePlaneObject(int i)
        {
            // Create planes for camera fustrum planes if he doesn't exist
            if (_planeObj[i] == null)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Plane);
                p.name = $"Plane {i}";
                p.GetComponent<MeshRenderer>().material = materials[i];
                p.transform.position = -_cameraPlanes[i].normal * _cameraPlanes[i].distance;
                p.transform.rotation = Quaternion.FromToRotation(Vector3.up, _cameraPlanes[i].normal);
                p.transform.localScale = Vector3.one * 100f; // set a huge scale for the planes
                _planeObj[i] = p;
            }
            else // otherwise update pos and rotation
            {
                _planeObj[i].transform.position = -_cameraPlanes[i].normal * _cameraPlanes[i].distance;
                _planeObj[i].transform.rotation = Quaternion.FromToRotation(Vector3.up, _cameraPlanes[i].normal);
            }
        }

        private void UpdatePlaneObject(int i)
        {
            _planeObj[i].transform.position = -_cameraPlanes[i].normal * _cameraPlanes[i].distance;
            _planeObj[i].transform.rotation = Quaternion.FromToRotation(Vector3.up, _cameraPlanes[i].normal);
        }
    }
}
