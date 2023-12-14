using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameManager: MonoBehaviour { // == main
    // TODO const static attributs
    List<string> _names = new List<string>() {
            "Right", "Left", "Up", "Down", "Back", "Front", "In", "Out" };
    List<string> _materials = new List<string>() {
            "Red", "Orange", "Blue", "Green", "Yellow", "White", "Purple", "Pink" };
    List<string> _circle_materials = new List<string>() {
            "XY", "XZ", "YZ", "XW", "YW", "ZW" };
    // ---

    public GameObject puzzle;
    // TODO move in another file?
    // TODO Create a specific struct?
    Puzzle p;

    private bool _cubeRotating = false;

    [SerializeField]
    private SelectSticker selectedSticker;
    // TODO for debug / test purpose?
    public int axis1 = 0;
    public int axis2 = 1;
    // ---

    // To customize the Rubik // TODO need to be added in a Parameter Menu
    [SerializeField]
    private Mesh sphereMesh;
    [SerializeField]
    private int puzzleSize = 2;
    private float stickerDistance = 10f;
    private float stickerSize = 0.125f;
    private float trailWidth = 0.0078125f;
    public float rotationSpeed = 2f;

    // to simplify the camera rotation
    // TODO move it in another file?
    static float s3 = 1f / Mathf.Sqrt(3f);
    static float s6 = (3f + Mathf.Sqrt(3f)) / 6f;
    static float _s6 = 1f - s6;

    static Matrix4x4 specialProjection = new Matrix4x4(
        new Vector4(-s6, 0f, _s6, s3),
        new Vector4(_s6, 0f, -s6, s3),
        new Vector4(-s3, 0f, -s3, -s3),
        new Vector4(0f, 1f, 0f, 0f));

    private int cameraRotationMode = 0;
    Matrix4x4 cameraRotation = specialProjection;

    // secondary rotation matrix to eventually use later
    Matrix4x4 colorAssignment = new Matrix4x4(
        new Vector4(1, 0, 0, 0),
        new Vector4(0, 1, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1));

    /// <summary>
    /// Awake is called automatically before the function Start
    /// </summary>
    void Awake() {
        puzzle = new GameObject();
        puzzle.name = "Puzzle";
        puzzle.tag = "Puzzle"; // Defines this object as a Puzzle object
        p = new Puzzle();

        // Find the 3D coordinates of the 4D stickers of the puzzle
        GenerateStickerCoordinates();

        // Create a GameObject for each point and link them in the GameObject "Puzzle"
        RenderStickers();

        // Create GameObjects representing the rotation axes, aesthetic purpose
        GameObject circleContainer = RenderCircles("CircleContainer");

        // Creates the dupe puzzle to display the classical view in a UI
        GameObject puzzleDuplicate = Instantiate(puzzle);
        puzzleDuplicate.name = "Puzzle_UI";
        SetLayerAllChildren(puzzleDuplicate.transform, 3); // Change layer for camera view
        ChangeProjection(); // Change projection to classical view to render the circles
        GameObject circleContainer_UI = RenderCircles("CircleContainer_UI");
        ChangeProjection(); // Change back projection for the first Update() frame cycle
        SetLayerAllChildren(circleContainer_UI.transform, 3);
    }

    /// <summary>
    /// Start is called automatically before the first frame update
    /// </summary>
    void Start(){
        // Handles rotation in parallel to the Update method
        StartCoroutine(RotationHandler());
    }

    /// <summary>
    /// Update is called automatically once per frame
    /// </summary>
    void Update() {
        if (!_cubeRotating) {
            if (Input.GetKeyDown(KeyCode.LeftShift)) {
                (axis1, axis2) = (axis2, axis1);
            }
            if (Input.GetKeyDown(KeyCode.R) && axis1 != axis2) {
                LaunchRotation();
            }
            // TODO: repair projection swap to swap projection views
            /*if (Input.GetKeyDown(KeyCode.P)) {
                ChangeProjection();
            }*/
        }
        // At all times, there are two puzzle game objects.
        // The first is the special projection, the second is the classic projection.
        // The loop below projects the stickers for the first, changes projection,
        // then projects the stickers for the second, then changes projection back
        // to prepare for the next frame.
        foreach (GameObject puzzle in GameObject.FindGameObjectsWithTag("Puzzle")) {
            PuzzleProjection4DTo3D(puzzle);
            ChangeProjection();
        }
    }

    /// <summary>
    /// initialize the data to lauch a rotation
    /// </summary>
    public void LaunchRotation() {
        _cubeRotating = true;
    }

    /// <summary>
    /// Create coordinates for each sticker
    /// </summary>
    void GenerateStickerCoordinates() {
        List<List<Vector4>> stickers = new List<List<Vector4>>();
        const int nbPoints = 8;
        for (int i = 0; i < nbPoints; i++) {
            Vector4 point = Vector4.zero;
            int pointIndex = Mathf.FloorToInt(i * 0.5f);
            int altSign = 1 - (2 * (i % 2));
            point[pointIndex] = altSign;

            stickers.Add(new List<Vector4>());
            for (int j = 0; j < Mathf.Pow(puzzleSize, 3); j++) {
                Vector3 temp = new Vector3(0, 0, 0);
                if (puzzleSize > 1) {
                    temp.x = Mathf.Lerp(-1f, 1f,
                        (Mathf.FloorToInt(j / Mathf.Pow(puzzleSize, 2)) % puzzleSize) / (puzzleSize - 1f));
                    temp.y = Mathf.Lerp(-1f, 1f,
                        (Mathf.FloorToInt(j / puzzleSize) % puzzleSize) / (puzzleSize - 1f));
                    temp.z = Mathf.Lerp(-1f, 1f,
                        (j % puzzleSize) / (puzzleSize - 1f));
                }
                Vector4 subpoint = new Vector4(0, 0, 0, 0);
                subpoint = InsertFloat(temp / stickerDistance, point[pointIndex], pointIndex);
                stickers[i].Add(subpoint);
            }
        }
        p.UpdateStickers(stickers);
    }

    /// <summary>
    /// Draw the circles on the 3D space
    /// </summary>
    void RenderStickers() {
        for (int i = 0; i < p.NbCells(); i++) {
            // TODO warning : length of _names and _materials may not be the same as the number of points
            GameObject cell = new GameObject();
            cell.name = _names[i];

            // place these points in the space
            cell.transform.parent = puzzle.transform;
            for (int j = 0; j < p.NbStickers(i); j++) {
                GameObject sticker = new GameObject();
                sticker.name = _names[i] + "_" + j;

                // add mesh
                sticker.AddComponent<MeshFilter>();
                sticker.GetComponent<MeshFilter>().mesh = sphereMesh;

                // add material
                Material stickerMat = Resources.Load(_materials[i], typeof(Material)) as Material;
                sticker.AddComponent<MeshRenderer>();
                sticker.GetComponent<Renderer>().material = stickerMat;

                // add the Select Scipt
                sticker.AddComponent<SelectSticker>();
                sticker.GetComponent<SelectSticker>().SetCoordinates(p.GetSticker(i,j));
                sticker.AddComponent<MeshCollider>();

                // place these points in the space
                sticker.transform.localScale = stickerSize * Vector3.one;
                sticker.transform.parent = cell.transform;
                sticker.transform.position = Projection4DTo3D(p.GetSticker(i, j));
            }
        }
    }

    /// <summary>
    /// Rotate a certain amount around a rotation plane and create vertices
    /// // TODO rephrase the doc + rename fn?
    /// </summary>
    /// <param name="stickers"></param>
    /// <param name="sticker"></param>
    /// <param name="vertices"></param>
    /// <param name="index"></param>
    /// <param name="axis1"></param>
    /// <param name="axis2"></param>
    /// <param name="angle"></param>
    /// <param name="makeVertices"></param>
    void TraverseAxis(List<Vector4> stickers, GameObject sticker, List<Vector3> vertices, int index, int axis1, int axis2,
            float angle, bool makeVertices = true) {
        sticker.transform.position = Projection4DTo3D(stickers[index]);
        if (makeVertices) {
            float vertexX = trailWidth * Mathf.Sin(angle);
            float vertexY = trailWidth * Mathf.Sin(angle);
            float vertexZ = trailWidth * Mathf.Cos(angle);
            vertices.Add(new Vector3(vertexX, vertexY, vertexZ) + sticker.transform.position);
            vertices.Add(new Vector3(-vertexX, vertexY, -vertexZ) + sticker.transform.position);
            vertices.Add(new Vector3(0, -vertexY, 0) + sticker.transform.position);
        }
        stickers[index] = RotationMatrix(axis1, axis2, angle) * stickers[index];
    }

    /// <summary>
    /// Create circle mesh from vertices
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    Mesh CreateCircleMesh(List<Vector3> vertices) {
        Mesh mesh = new Mesh();

        // add vertices
        mesh.vertices = vertices.ToArray();

        // create uvs
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 1; i < vertices.Count; i++) {
            if (i % 2 == 0)
                uvs[i] = new Vector2(i / (vertices.Count - 2f), 0);
            else
                uvs[i] = new Vector2((i - 1) / (vertices.Count - 2f), 1);
        }
        mesh.uv = uvs;

        // create triangles
        int[] triangles = new int[vertices.Count * 6];
        int tri = 0;
        for (int i = 0; i < vertices.Count * 2; i++) {
            int shift = Mathf.FloorToInt(i / 6) * 3;
            int j = i - shift;
            switch (i % 6) {
                case 0:
                    triangles[tri] = (j + 0) % vertices.Count;
                    triangles[tri + 1] = (j + 1) % vertices.Count;
                    triangles[tri + 2] = (j + 3) % vertices.Count;
                    break;
                case 1:
                    triangles[tri] = (j + 0) % vertices.Count;
                    triangles[tri + 1] = (j + 3) % vertices.Count;
                    triangles[tri + 2] = (j + 2) % vertices.Count;
                    break;
                case 2:
                    triangles[tri] = (j - 1) % vertices.Count;
                    triangles[tri + 1] = (j + 0) % vertices.Count;
                    triangles[tri + 2] = (j + 2) % vertices.Count;
                    break;
                case 3:
                    triangles[tri] = (j - 1) % vertices.Count;
                    triangles[tri + 1] = (j + 2) % vertices.Count;
                    triangles[tri + 2] = (j + 1) % vertices.Count;
                    break;
                case 4:
                    triangles[tri] = (j - 2) % vertices.Count;
                    triangles[tri + 1] = (j - 4) % vertices.Count;
                    triangles[tri + 2] = (j + 1) % vertices.Count;
                    break;
                case 5:
                    triangles[tri] = (j - 5) % vertices.Count;
                    triangles[tri + 1] = (j - 2) % vertices.Count;
                    triangles[tri + 2] = (j + 0) % vertices.Count;
                    break;
                default:
                    break;
            }
            tri += 3;
        }
        mesh.triangles = triangles;

        return mesh;
    }

    /// <summary>
    /// Create circle from mesh
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="axisIndex"></param>
    /// <param name="tempstickerIndex"></param>
    /// <returns></returns>
    GameObject CreateCircle(Mesh mesh, int axisIndex, int tempstickerIndex) {
        // create gameobject
        GameObject circle = new GameObject();
        circle.name = _circle_materials[axisIndex] + "_" + tempstickerIndex;

        // add mesh
        circle.AddComponent<MeshFilter>();
        circle.GetComponent<MeshFilter>().mesh = mesh;

        // add material
        Material circleMat = Resources.Load(_circle_materials[axisIndex], typeof(Material)) as Material;
        circle.AddComponent<MeshRenderer>();
        circle.GetComponent<Renderer>().material = circleMat;

        return circle;
    }

    /// <summary>
    /// Draw the axis circles on the 3D space
    /// </summary>
    GameObject RenderCircles(string name) {
        List<Vector4> tempstickers = new List<Vector4>();
        List<Vector3> vertices = new List<Vector3>();

        // copy position from actual stickers // TODO?
        for (int i = 0; i < p.NbStickers(0); i++) {
            tempstickers.Add(p.GetSticker(0, i));
        }

        // create circles
        List<Tuple<int, int>> rotationAxes = new List<Tuple<int, int>>() {
            Tuple.Create(0, 1), Tuple.Create(0, 2), Tuple.Create(1, 0),
            Tuple.Create(2, 1), Tuple.Create(1, 3), Tuple.Create(3, 1),
            Tuple.Create(2, 3), Tuple.Create(0, 3)
        };
        List<int> matChoice = new List<int>() { 0, 1, 0, 2, 4, 4, 5, 3 };
        GameObject circleContainer = new GameObject();
        circleContainer.name = name;
        for (int i = 0; i < tempstickers.Count; i++) {
            GameObject tempsticker = new GameObject();
            // for all rotations necessary to roam all 6 circles
            for (int j = 0; j < 8; j++) {
                switch (j) {
                    // rotation j = 2 and j = 5 are only to get on the right circle
                    case 2:
                    case 5:
                        TraverseAxis(tempstickers, tempsticker, vertices, i,
                            rotationAxes[j].Item1, rotationAxes[j].Item2, 90f, false);
                        break;
                    // other rotations draw the circles
                    default:
                        for (int k = 0; k < 90; k++) {
                            TraverseAxis(tempstickers, tempsticker, vertices, i,
                                rotationAxes[j].Item1, rotationAxes[j].Item2, 4f);
                        }
                        Mesh circleMesh = CreateCircleMesh(vertices);
                        GameObject circle = CreateCircle(circleMesh, matChoice[j], i);
                        circle.transform.parent = circleContainer.transform;
                        vertices.Clear();
                        break;
                }
            }
            Destroy(tempsticker);
        }
        return circleContainer;
    }

    /// <summary>
    /// Inserts value in Vector3 at pos, making it a Vector4
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="value"></param>
    /// <param name="pos"></param>
    /// <returns>Vector4 with value inserted at index pos</returns>
    Vector4 InsertFloat(Vector3 vec, float value, int pos) {
        pos = Mathf.Clamp(pos, 0, 3);
        Vector4 result = Vector4.zero;
        switch (pos) {
            case 0:
                result = new Vector4(value, vec.x, vec.y, vec.z);
                break;
            case 1:
                result = new Vector4(vec.x, value, vec.y, vec.z);
                break;
            case 2:
                result = new Vector4(vec.x, vec.y, value, vec.z);
                break;
            case 3:
                result = new Vector4(vec.x, vec.y, vec.z, value);
                break;
            default:
                break;
        }
        return result;
    }

    /// <summary>
    /// Generate a new rotationMatrix from two axis and an angle
    /// </summary>
    /// <param name="axis1"></param>
    /// <param name="axis2"></param>
    /// <param name="angle"></param>
    public Matrix4x4 RotationMatrix(int axis1, int axis2, float angle) {
        Matrix4x4 rotationMatrix = Matrix4x4.identity;
        rotationMatrix[axis1, axis1] = Mathf.Cos(angle * Mathf.Deg2Rad);
        rotationMatrix[axis2, axis1] = -Mathf.Sin(angle * Mathf.Deg2Rad);
        rotationMatrix[axis1, axis2] = Mathf.Sin(angle * Mathf.Deg2Rad);
        rotationMatrix[axis2, axis2] = Mathf.Cos(angle * Mathf.Deg2Rad);
        return rotationMatrix;
    }

    public IEnumerator RotationHandler() {
        while (true) {
            if (!_cubeRotating) {
                yield return null;
                // == continue; in c, to avoid freeze screen when used in coroutine
            }
            else {
                List<List<Vector4>> targets = DefineTargets();
                if (IsBetweenRangeExcluded(rotationSpeed, 0f, 90f)) {
                    float totalRotation = 0;
                    while (Mathf.Abs(90f - totalRotation) > Mathf.Epsilon) {
                        totalRotation = RotateOverTime(rotationSpeed, totalRotation);
                        yield return null;
                    }
                }

                SnapToTargets(targets);
                _cubeRotating = false;
            }
        }
    }

    string whosOpposite(string sphereName) {
        int index = _names.IndexOf(sphereName);
        return (index % 2 == 0) ? _names[index + 1] : _names[index - 1];
    }

    public List<string> whosGunnaRotate(string sphereName) { // TODO remove public?
        // TODO not complete yet?
        List<string> mustRotate = new List<string>();
        string opposite = whosOpposite(sphereName);
        foreach (string entry in _names) {
            if (entry != sphereName & entry != opposite) {
                mustRotate.Add(entry);
            }
        }
        return mustRotate;
    }

    /// <summary>
    /// Determine the destination of each cell and sticker
    /// </summary>
    public List<List<Vector4>> DefineTargets() {
        // TODO put "puzzle" in param?
        // TODO need change for differents layers
        List<List<Vector4>> targets = new List<List<Vector4>>(); // TODO may be simplified with List<Vector4>?
        Matrix4x4 rotate = RotationMatrix(axis1, axis2, 90);

        for (int i = 0; i < puzzle.transform.childCount; i++) { // TODO change conditions
            targets.Add(new List<Vector4>());

            Transform cell = puzzle.transform.GetChild(i);
            for (int j = 0; j < cell.childCount; j++) {
                targets[i].Add(rotate * p.GetSticker(i, j));
            }
        }
        return targets;
    }

    /// <summary>
    /// Rotates by 90 degrees with animation
    /// </summary>
    /// <param name="rotationSpeed"> </param>
    public float RotateOverTime(float rotationSpeed, float totalRotation) {
        // TODO need optimization?
        Matrix4x4 rotate = RotationMatrix(axis1, axis2, rotationSpeed);
        rotationSpeed = Mathf.Clamp(rotationSpeed, 0f, 90f - totalRotation);
        totalRotation = Mathf.Clamp(totalRotation + rotationSpeed, 0f, 90f);
        for (int i = 0; i < puzzle.transform.childCount; i++) {
            Transform cell = puzzle.transform.GetChild(i);
            for (int j = 0; j < cell.childCount; j++) {
                Transform sticker = cell.GetChild(j);
                p.setSticker(i, j, rotate * p.GetSticker(i, j));
            }
        }
        return totalRotation;
    }

    /// <summary>
    /// Snaps each cell sticker to its final position
    /// </summary>
    public void SnapToTargets(List<List<Vector4>> targets) {
        for (int i = 0; i < puzzle.transform.childCount; i++) {
            Transform cell = puzzle.transform.GetChild(i);
            for (int j = 0; j < cell.childCount; j++) {
                Transform sticker = cell.GetChild(j);
                p.setSticker(i, j, targets[i][j]);
            }
        }
    }

    /// <summary>
    /// Toggle between classic projection and special projection
    /// </summary>
    private void ChangeProjection() {
        switch (cameraRotationMode) {
            case 0:
                cameraRotationMode = 1;
                cameraRotation = Matrix4x4.identity;
                break;
            case 1:
            default:
                cameraRotationMode = 0;
                cameraRotation = specialProjection;
                break;
        }
        // TODO: find a better way to manage cameraRotation

        // Destroy previous circles
        // GameObject circleContainer = GameObject.Find("CircleContainer");
        // Destroy(circleContainer);

        // Render new circles
        // RenderCircles();

        // Project all 4D stickers to 3D space
        // PuzzleProjection4DTo3D(gameObject);
    }

    /// <summary>
    /// Projects a GameObject and all its 4D children into 3D
    /// </summary>
    /// <param name="gameObject"></param>
    private void PuzzleProjection4DTo3D(GameObject gameObject) {
        for (int i = 0; i < gameObject.transform.childCount; i++) {
            Transform cell = gameObject.transform.GetChild(i);
            for (int j = 0; j < cell.childCount; j++) {
                Transform sticker = cell.GetChild(j);
                sticker.position = Projection4DTo3D(p.GetSticker(i, j));
            }
        }
    }

    public static bool IsBetweenRangeExcluded(float value, float value1, float value2) {
        return value > Mathf.Min(value1, value2) && value < Mathf.Max(value1, value2);
    }

    /// <summary>
    /// Projects a 4D vector into 3D
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Vector4 Projection4DTo3D(Vector4 point) {
        Vector4 temp = new Vector4(point.x, point.y, point.z, point.w);
        temp = cameraRotation * colorAssignment * temp;
        Vector3 projected = Vector3.zero;

        // Handle projection to infinity
        if (temp.w + 1 != 0) {
            projected = new Vector3(temp.x, temp.y, temp.z) / (temp.w + 1);
        }
        else {
            projected = new Vector3(
                Mathf.Sign(temp.x) * Int32.MaxValue,
                Mathf.Sign(temp.y) * Int32.MaxValue,
                Mathf.Sign(temp.z) * Int32.MaxValue);
        }
        return projected;
    }

    public int GetAxis1() {
        return axis1;
    }

    public int GetAxis2() {
        return axis2;
    }

    public bool GetRotateFlag() {
        return _cubeRotating;
    }

    public void setterSelection(SelectSticker selection){
        selectedSticker = selection;
    }

    public SelectSticker GetSelection(){
        return selectedSticker;
    }

    /// <summary>
    /// set the plane based on two axis
    /// </summary>
    /// <param name="a1">the first axis</param>
    /// <param name="a2">the second axis</param>
    public void SetPlane(int a1, int a2) {
        axis1 = a1;
        axis2 = a2;
    }

    /// <summary>
    /// Sets display Layer of a transform and all its children
    /// </summary>
    /// <param name="root"></param>
    /// <param name="layer"></param>
    void SetLayerAllChildren(Transform root, int layer) {
        root.gameObject.layer = layer;
        var children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (var child in children) {
            //Debug.Log(child.name);
            child.gameObject.layer = layer;
        }
    }

    // void BaseRotation(GameObject sphere, string input) {
    //     if (input == "y") {
    //         rotationMatrix[0, 0] = Mathf.Cos(0.1f);
    //         rotationMatrix[2, 0] = -Mathf.Sin(0.1f);
    //         rotationMatrix[0, 2] = Mathf.Sin(0.1f);
    //         rotationMatrix[2, 2] = Mathf.Cos(0.1f);
    //     }
    //     if (input == "x") {
    //         rotationMatrix[1, 1] = Mathf.Cos(0.1f);
    //         rotationMatrix[2, 1] = -Mathf.Sin(0.1f);
    //         rotationMatrix[1, 2] = Mathf.Sin(0.1f);
    //         rotationMatrix[2, 2] = Mathf.Cos(0.1f);
    //     }
    //     if (input == "z") {
    //         rotationMatrix[0, 0] = Mathf.Cos(0.1f);
    //         rotationMatrix[1, 0] = -Mathf.Sin(0.1f);
    //         rotationMatrix[0, 1] = Mathf.Sin(0.1f);
    //         rotationMatrix[1, 1] = Mathf.Cos(0.1f);
    //     }
    //     Vector3 sphereCoords = sphere.transform.position;
    //     sphereCoords = rotationMatrix * sphereCoords;
    //     sphere.transform.position = sphereCoords;
    // }

    // void BigRotation(GameObject sphere, string input) {
    //     List<string> toBeRotated = new List<string>(6);
    //     toBeRotated = whosGunnaRotate(sphere.name);
    //     foreach (string entry in toBeRotated) {
    //         baseRotation(GameObject.Find(entry),input);
    //     }
    // }
}
