using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SphereProjection4D : MonoBehaviour
{
    List<Vector4> _points = new List<Vector4>();
    List<List<Vector4>> _subpoints = new List<List<Vector4>>();
    List<Vector4> targets = new List<Vector4>();
    Matrix4x4 rotationMatrix = Matrix4x4.identity; // TODO à voir avec M. Nozick
    bool cubeRotating = false;
    List<string> _names = new List<string>() {
            "Right", "Left", "Up", "Down", "Back", "Front", "In", "Out" };
    List<string> _materials = new List<string>() {
            "Red", "Orange", "Blue", "Green", "Yellow", "White", "Purple", "Pink" };

    [SerializeField]
    private Mesh sphereMesh;
    [SerializeField]
    GameObject container;
    [SerializeField]
    float rotationSpeed = 2f;
    [SerializeField]
    int axis1 = 0;
    [SerializeField]
    int axis2 = 1;
    private float totalRotation = 0;
    [SerializeField]
    private int puzzleDimension = 2;

    static float s3 = 1f / Mathf.Sqrt(3f);
    static float s6 = (3f + Mathf.Sqrt(3f)) / 6f;
    static float _s6 = 1f - s6;

    Matrix4x4 cameraRotation = new Matrix4x4(
        new Vector4(-s6, 0f, _s6, s3),
        new Vector4(_s6, 0f, -s6, s3),
        new Vector4(-s3, 0f, -s3, -s3),
        new Vector4(0f, 1f, 0f, 0f));

    Matrix4x4 colorAssignment = new Matrix4x4(
        new Vector4(1, 0, 0, 0),
        new Vector4(0, 1, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1));

    void UpdateRotationMatrix(int axis1, int axis2, float angle) {
        rotationMatrix = Matrix4x4.identity;
        rotationMatrix[axis1, axis1] = Mathf.Cos(angle * Mathf.Deg2Rad);
        rotationMatrix[axis2, axis1] = -Mathf.Sin(angle * Mathf.Deg2Rad);
        rotationMatrix[axis1, axis2] = Mathf.Sin(angle * Mathf.Deg2Rad);
        rotationMatrix[axis2, axis2] = Mathf.Cos(angle * Mathf.Deg2Rad);
    }

    // Start is called before the first frame update
    void Start() {
        // Generate points
        const int nbPoints = 8;

        for (int i = 0; i < nbPoints; i++) {
            Vector4 point = new Vector4(0, 0, 0, 0);
            print(i);
            point[Mathf.FloorToInt(i / 2)] = 1 - (2 * (i % 2));
            print(point);
            _points.Add(point);

        }

        // Create a GameObject for each point and link them in the GameObject "container"
        container.name = "Container";

        for (int i = 0; i < _points.Count; i++) {
            // TODO warning : length of _names and _materials may not be the same as the number of points
            GameObject cell = new GameObject();
            cell.name = _names[i];

            cell.AddComponent<MeshFilter>();
            cell.GetComponent<MeshFilter>().mesh = sphereMesh;

            Material sphereMat = Resources.Load(_materials[i], typeof(Material)) as Material;
            cell.AddComponent<MeshRenderer>();
            cell.GetComponent<Renderer>().material = sphereMat;

            // place these points in the space
            cell.transform.localScale = 0.2f * Vector3.one;
            cell.transform.parent = container.transform;
            cell.transform.position = Projection4DTo3D(_points[i]);

        }

        // TODO for test purpose, must be deleted later
        UpdateRotationMatrix(axis1, axis2, 0);

        // To make animation
        StartCoroutine(Rotate90Degrees());
    }

    // Update is called once per frame
    void Update() {
        if (!cubeRotating) {
            if (Input.GetKeyDown(KeyCode.LeftShift)) {
                (axis1, axis2) = (axis2, axis1);
            }
            if (Input.GetKeyDown(KeyCode.R) && axis1 != axis2) {
                targets.Clear();
                totalRotation = 0;
                cubeRotating = true;
            }
        }
    }

    private IEnumerator Rotate90Degrees() {
        while (true) {
            if (!cubeRotating) {
                yield return null;
            }
            else {
                int i = 0;
                UpdateRotationMatrix(axis1, axis2, 90);
                foreach (Transform child in container.transform) {
                    targets.Add(rotationMatrix * _points[i]);
                    i++;
                }
                if (IsBetweenRangeExcluded(rotationSpeed, 0f, 90f)) {
                    float rotationSpeedTemp = rotationSpeed;
                    while (Mathf.Abs(90f - totalRotation) > Mathf.Epsilon) {
                        totalRotation += rotationSpeedTemp;
                        rotationSpeedTemp = Mathf.Clamp(rotationSpeed, 0f, 90f - totalRotation + rotationSpeedTemp);
                        totalRotation = Mathf.Clamp(totalRotation, 0f, 90f);
                        i = 0;
                        UpdateRotationMatrix(axis1, axis2, rotationSpeed);
                        foreach (Transform child in container.transform) {
                            _points[i] = rotationMatrix * _points[i];
                            child.transform.position = Projection4DTo3D(_points[i]);
                            i++;
                        }
                        yield return null;
                    }
                }
                i = 0;
                foreach (Transform child in container.transform) {
                    _points[i] = targets[i];
                    child.transform.position = Projection4DTo3D(_points[i]);
                    i++;
                }
                cubeRotating = false;
            }
        }
    }

    public static bool IsBetweenRangeExcluded(float value, float value1, float value2) {
        return value > Mathf.Min(value1, value2) && value < Mathf.Max(value1, value2);
    }

    Vector4 Projection4DTo3D(Vector4 point) {
        Vector4 temp = new Vector4(point.x, point.y, point.z, point.w);
        temp = cameraRotation * colorAssignment * temp;
        return new Vector3(temp[0], temp[1], temp[2]) / (temp[3] + 1);
    }

    List<float> AddFloatList(List<float> a, List<float> b) {
        List<float> result = new List<float>();
        for (int i = 0; i < a.Count; i++) {
            result.Add(a[i] + b[i]);
        }
        return result;
    }

    private void RotateAroundTowards(Transform a, Vector3 b, Vector3 center, int direction, float t) {
        float radius = Vector3.Distance(center, b);
        float a_angle = Mathf.Atan2(a.position.z - center.z, a.position.x - center.x) * Mathf.Rad2Deg;
        float b_angle = Mathf.Atan2(b.z - center.z, b.x - center.x) * Mathf.Rad2Deg;
        if (direction * b_angle > direction * a_angle) {
            b_angle = b_angle - 360 * direction;
        }
        a_angle = Mathf.Lerp(a_angle, b_angle, t);
        a.position = new Vector3(Mathf.Cos(a_angle * Mathf.Deg2Rad) * radius + center.x, a.position.y,
            Mathf.Sin(a_angle * Mathf.Deg2Rad) * radius + center.z);
    }
}
