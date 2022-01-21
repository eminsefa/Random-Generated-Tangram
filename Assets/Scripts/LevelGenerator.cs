using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class LevelGenerator : MonoBehaviour
{
    [Tooltip("Shape count is limited between 6 to 12")] [SerializeField]
    private int shapeCount;

    [SerializeField] private Transform spawnArea;
    [SerializeField] private Transform dotObjHolder;
    [SerializeField] private Transform shapeObjectHolder;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private List<Material> shapeMats;

    private List<GameObject> _triangles;
    private List<GameObject> _copyTriangleList = new List<GameObject>();
    private Dictionary<int, List<GameObject>> _shapeTrianglesDictionary;
    private List<int> _lastShapeTriangleNumbers;
    private float _playAreaScale;
    private int _triangleAdded;
    private int _iteration;
    private int _lineTriangleCount;
    private bool _onEditorTest;
    private const int GridSize = 3;

    /// <summary>
    /// This script generates the level based on grid size:
    /// -Creates the grids with dots on play area related to area scale.
    /// -Creates triangles with modular rule. Each 4 types of triangle(to create a square) returns the same modular .
    /// -4 triangles are scaled to create a half grid square.
    /// -Squares are ordered as line to columns.
    /// -To create a shape algorithm starts with a random triangle. Then follows neighbour rules based on grid size and modular.
    /// -When desired number of shapes are created, each shape combines triangles and creates a new mesh.
    /// -Difficulty increased automatically by increased desired shape count.
    /// -Level generator can be tested in editor by changing shape count and grid size(They need to be related to prevent too big or too small shapes).
    /// </summary>
    ///----Notes----
    /// -If pieces' shape are desired to be similar to constant shapes, more rules can be added to algorithm based on line and column numbers.
    /// -First tried to create shapes by ordering matrix with line and column size as all shapes can be created this way. But found neighbour algorithm more simple.
    /// -If asset use were preferred, would try to use ray fire or mesh slicer to cut a plane to smooth shapes.
    /// -To optimize; algorithm rules can be converted to first stage: instead of creating the triangle objects, end shapes can be created directly with mesh triangles.
    private void Awake()
    {
        Application.targetFrameRate = 60;
        DestroyEditorTestObjects();
        SetLevel(false);
    }

    private void DestroyEditorTestObjects()
    {
        foreach (var c in dotObjHolder.GetComponentsInChildren<Transform>())
        {
            if (c != null && c != dotObjHolder.transform)
            {
                if (_onEditorTest) DestroyImmediate(c.gameObject);
                else Destroy(c.gameObject);
            }
        }

        if (_onEditorTest)
        {
            foreach (var c in shapeObjectHolder.GetComponentsInChildren<Transform>())
            {
                if (c != null && c != shapeObjectHolder.transform)
                {
                    DestroyImmediate(c.gameObject);
                }
            }
        }
        else
        {
            foreach (var c in shapeObjectHolder.GetComponentsInChildren<Transform>())
            {
                if (c != null && c != shapeObjectHolder.transform)
                {
                    Destroy(c.gameObject);
                }
            }
        }
    }

    private void Start()
    {
        Helper.LevelCompleted += LevelCompleted;
    }

    private void OnDisable()
    {
        Helper.LevelCompleted -= LevelCompleted;
    }

    private void LevelCompleted()
    {
        if (shapeCount < 12) shapeCount++;
        CreateTriangles();
    }

    public void SetLevel(bool onEditorTest = true)
    {
        _onEditorTest = onEditorTest;
        if (onEditorTest) DestroyEditorTestObjects();
        SetAreaDots();
        CreateTriangles();
    }

    private void SetAreaDots()
    {
        //Fills area with dots based on play area's scale

        var scale = spawnArea.localScale.x; //axis doesn't matter since it is a square
        var dotDistance = scale / GridSize;
        var iCount = 0;
        for (var i = -scale / 2; i <= scale / 2 + 0.01f; i += dotDistance / 4) //+0.01f for float decimal error
        {
            iCount++;
            var jCount = 0;
            for (var j = -scale / 2; j <= scale / 2 + 0.01f; j += dotDistance / 4)
            {
                jCount++;
                var spawnPos = spawnArea.TransformPoint(new Vector3(i, j, -0.5f));
                var dot = Instantiate(dotPrefab, spawnPos, dotPrefab.transform.rotation);
                dot.transform.SetParent(dotObjHolder);

                //Hide middle and corner dots, need them to ray cast if the point is filled
                //Could create a list here instead
                if (Mathf.Abs(Mathf.Abs(i) - 0.5f) < 0.01f || Mathf.Abs(Mathf.Abs(j) - 0.5f) < 0.01f
                                                           || iCount % 4 == 1 || jCount % 4 == 1
                                                           || iCount % 2 == 0 && iCount != 1 &&
                                                           iCount != (GridSize * 2) + 1
                                                           || jCount % 2 == 0 && jCount != 1 &&
                                                           jCount != (GridSize * 2) + 1)
                    dot.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }

    private void CreateTriangles()
    {
        if (shapeCount > 12) shapeCount = 12;
        if (shapeCount < 6) shapeCount = 6;
        _triangles = new List<GameObject>();
        _lastShapeTriangleNumbers = new List<int>();
        _iteration = 0;
        shapeObjectHolder.transform.position = Vector3.zero;

        //To prevent null exception
        _shapeTrianglesDictionary = new Dictionary<int, List<GameObject>>();
        for (int i = 0; i < shapeCount; i++)
        {
            _shapeTrianglesDictionary.Add(i, new List<GameObject>());
        }

        _triangles = new List<GameObject>();
        _playAreaScale = spawnArea.lossyScale.x;
        var dist = _playAreaScale / (GridSize * 2f);
        for (float i = 0; i < _playAreaScale - 0.01f; i += dist) //0.01f for float decimal error
        {
            for (float j = 0; j < _playAreaScale - 0.01f; j += dist)
            {
                //Move Parents will order triangles and get destroyed
                var moveParent = new GameObject().transform;
                CreateSquares(moveParent);
                moveParent.position = new Vector3(i, j, -0.5f);
            }
        }

        _lineTriangleCount = _triangles.Count / (GridSize * 2);

        OrderTriangleList();

        if (_onEditorTest) shapeObjectHolder.position = new Vector3(-0.5f, 1, 0);
        else shapeObjectHolder.position = new Vector3(-0.5f, 15, 0);
        _copyTriangleList = _triangles.ToList();

        Helper.HalfGridAxisSize = _playAreaScale / (GridSize * 2);

        //Start algorithm with a number triangle
        SelectTriangle(Random.Range(0, _triangles.Count));
    }

    private void CreateSquares(Transform moveParent)
    {
        //Creating a square with triangles in mod order

        for (int i = 0; i < 4; i++)
        {
            var verts = Helper.GetVertices(i);
            var tris = new[] { 0, 1, 2 };

            var m = new Mesh()
            {
                vertices = verts,
                triangles = tris
            };
            var obj = new GameObject();
            var objTr = obj.transform;
            objTr.localScale = Vector3.one * _playAreaScale / (GridSize * 4);

            var rend = obj.AddComponent<MeshRenderer>();
            rend.material = spawnArea.GetComponent<MeshRenderer>().sharedMaterial;
            var filter = obj.AddComponent<MeshFilter>();
            filter.mesh = m;

            //Creating a temporary parent and positioning to center
            var sum = Vector3.zero;
            for (int s = 0; s < 3; s++) sum += objTr.TransformPoint(verts[s]);
            var result = sum / 3;
            var parent = new GameObject("Parent" + (i / 3)).transform;
            parent.position = result;

            objTr.SetParent(parent);
            _triangles.Add(obj);

            //Adding parents to a temporary parent to order list with squares
            moveParent.position = new Vector3(1, 1, 0);
            moveParent.SetParent(shapeObjectHolder);
            parent.SetParent(moveParent);
        }
    }

    private void OrderTriangleList()
    {
        //Order triangles by move parents position. Modular rules and listings are calculated by this order

        _triangles = _triangles.OrderBy(x => -x.transform.parent.parent.localPosition.y).ToList();
        for (int i = 0; i < _lineTriangleCount / 4; i++)
        {
            //Creating a partial list to order lines seperated
            var partialList = _triangles.GetRange(i * _lineTriangleCount, _lineTriangleCount);
            partialList = partialList.OrderBy(x => x.transform.parent.parent.localPosition.x).ToList();
            for (int j = 0; j < _lineTriangleCount; j++)
            {
                _triangles[(i * _lineTriangleCount) + j] = partialList[j];
            }
        }

        //After ordering list destroy parents
        for (int i = 0; i < _triangles.Count; i++)
        {
            var obj = _triangles[i];
            var moveParent = obj.transform.parent.parent.gameObject;
            obj.transform.SetParent(shapeObjectHolder);
            if (i != 0 && i % 4 == 3)
            {
                if (_onEditorTest) DestroyImmediate(moveParent);
                else Destroy(moveParent);
            }
        }
    }

    private void SelectTriangle(int number)
    {
        //Start with a triangle and add neighbour triangles to create a shape

        if (_triangles[number]) AddTriangle(number);

        //Find the next triangle
        var neighbourList = Helper.GetTriangleNeighbourList(number, _lineTriangleCount);
        var nextNumber = 0;
        neighbourList = neighbourList.OrderBy(a => Guid.NewGuid()).ToList(); //Randomize neighbour triangle list
        for (int i = 0; i < 3; i++)
        {
            var addedNumber = number + neighbourList[i];
            var connected = number / _lineTriangleCount == addedNumber / _lineTriangleCount ||
                            Mathf.Abs(number - addedNumber) == _lineTriangleCount - 2;
            if (addedNumber < _triangles.Count && addedNumber > 0 && connected) //Check if neighbour is connected
            {
                if (_triangles[addedNumber] != null)
                {
                    nextNumber = addedNumber;
                    break;
                }
            }
        }

        var rand = Random.Range(-_lineTriangleCount / 4, _lineTriangleCount / 4) /
                   2; //To randomize max triangle per shape
        if (_triangleAdded >= (_triangles.Count / shapeCount) - rand)
        {
            IterationCompleted();
        }
        else
        {
            if (nextNumber != 0) SelectTriangle(nextNumber);
            else
            {
                if (_lastShapeTriangleNumbers.Contains(number))
                    _lastShapeTriangleNumbers.Remove(number); //To prevent picking self

                //Check if shape is completely surrounded
                if (_lastShapeTriangleNumbers.Count == 0)
                {
                    IterationCompleted();
                    return;
                }

                //Try another neighbour from current shape's another triangle
                var newTry = Random.Range(0, _lastShapeTriangleNumbers.Count);
                SelectTriangle(_lastShapeTriangleNumbers[newTry]);
            }
        }
    }

    private void AddTriangle(int number)
    {
        //Add triangle to a shape

        _triangleAdded++;
        _lastShapeTriangleNumbers.Add(number);
        _shapeTrianglesDictionary[_iteration].Add(_triangles[number]);
        _triangles[number] = null;
    }

    private void IterationCompleted()
    {
        //A shape iteration is completed, check to create a new one or end the algorithm

        _iteration++;
        _triangleAdded = 0;
        _lastShapeTriangleNumbers = new List<int>();
        FillIterationSpaces(); //Doing this here to prevent small spaces every iteration

        if (_iteration == shapeCount)
        {
            if (_triangles.Count > 0) FillAloneTriangles();
            return;
        }

        //If not enough shape is created, start from random triangle to create another shape
        for (int i = 0; i < _triangles.Count; i++)
        {
            if (_triangles[i])
            {
                SelectTriangle(i);
                break;
            }
        }
    }

    private void FillIterationSpaces()
    {
        //Fill empty spaces created when iteration ended. This prevents creating new shapes for little spaces

        for (int i = 0; i < _triangles.Count; i++)
        {
            if (_triangles[i]) //Check if the triangle is not filled
            {
                var neighbourList = Helper.GetTriangleNeighbourList(i, _lineTriangleCount);
                var nonFilledNumbers = new List<int>();
                foreach (var n in neighbourList)
                {
                    var number = i + n;
                    if (number < 0 || number >= _triangles.Count) continue;
                    if (_triangles[number])
                    {
                        nonFilledNumbers.Add(number);
                        if (nonFilledNumbers.Count > 1) return; //1 is maximum non filled neighbour count
                    }

                    //Fill connected empty triangles to a neighbour shape
                    var connected = i / _lineTriangleCount == number / _lineTriangleCount ||
                                    Mathf.Abs(i - number) == _lineTriangleCount - 2;
                    if (!nonFilledNumbers.Contains(number) && connected)
                    {
                        nonFilledNumbers.Add(i);
                        for (int k = 0; k < nonFilledNumbers.Count; k++)
                        {
                            var iter = _shapeTrianglesDictionary //Gets iteration number of connected shape
                                .FirstOrDefault(x => x.Value.Contains(_copyTriangleList[number])).Key;
                            _shapeTrianglesDictionary[iter].Add(_triangles[nonFilledNumbers[k]]);
                            _triangleAdded++;
                            _triangles[nonFilledNumbers[k]] = null;
                        }

                        break;
                    }
                }
            }
        }
    }

    private void FillAloneTriangles()
    {
        //Fills all spaces left after iteration reached max shape count
        //Finds all empty triangles, connects them to a neighbour shape

        for (int i = 0; i < _triangles.Count; i++)
        {
            if (_triangles[i])
            {
                var neighbourList = Helper.GetTriangleNeighbourList(i, _lineTriangleCount);
                for (int j = 0; j < neighbourList.Count; j++)
                {
                    var number = i + neighbourList[j];
                    if (number < 0 || number >= _triangles.Count) continue;
                    var connected = i / _lineTriangleCount == number / _lineTriangleCount ||
                                    Mathf.Abs(i - number) == _lineTriangleCount - 2;
                    if (!_triangles[number] && connected)
                    {
                        var iter = _shapeTrianglesDictionary //Gets iteration number of connected shape
                            .FirstOrDefault(x => x.Value.Contains(_copyTriangleList[number])).Key;
                        _shapeTrianglesDictionary[iter].Add(_triangles[i]);
                        _triangles[i] = null;
                        break;
                    }
                }
            }
        }

        //Check if any triangle is left because of list order
        var leftTriangles = _triangles.Where(x => x != null).ToList();
        if (leftTriangles.Count > 0) FillAloneTriangles();
        else
        {
            CreateShapes();
        }
    }

    private void CreateShapes()
    {
        //For each iteration listed triangles are combined to create one shape

        for (int i = 0; i < _shapeTrianglesDictionary.Keys.Count; i++)
        {
            var count = _shapeTrianglesDictionary[i].Count;
            var combine = new CombineInstance[count];
            for (int j = 0; j < count; j++)
            {
                var t = _shapeTrianglesDictionary[i][j];
                var tMesh = t.GetComponent<MeshFilter>().sharedMesh;
                combine[j].mesh = tMesh;
                combine[j].transform = t.transform.localToWorldMatrix;
                if (_onEditorTest) DestroyImmediate(t);
                else Destroy(t);
            }

            var obj = new GameObject();
            var filter = obj.AddComponent<MeshFilter>();
            var rend = obj.AddComponent<MeshRenderer>();
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.material = shapeMats[i];
            obj.name = "Shape " + i;

            filter.sharedMesh = new Mesh();
            filter.sharedMesh.CombineMeshes(combine, true);
            filter.sharedMesh.RecalculateNormals();

            obj.transform.SetParent(shapeObjectHolder);
            obj.AddComponent<Shape>();
        }
    }
}