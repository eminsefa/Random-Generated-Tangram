using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public struct Helper
{
    /// <summary>
    /// This script returns calculations and controls levels.
    /// Dots are created by Level Generator.
    /// After a shape is dropped on a position, dots check if the positions are filled.
    /// After all dot positions are filled level is completed.
    /// After level is completed piece count is increased and level is recreated.
    /// </summary>
    
    ///---Notes---
    /// Raycast was the first technique I tried. Grid system would work better.
    /// Even the scene is empty it returns 30 fps. Couldn't find the reason.
    public static event Action LevelCompleted;
    public static float HalfGridAxisSize { get; set; }
    public static int DotCount { get; set; }
    private static int _dotFilledCount;

    #region GetVertices
    public static Vector3[] GetVertices(int i)
    {
        //Returns vertex points of triangles in a modular order
        
        var verts = new Vector3[3];
        if (i == 0) //Left Triangle => mod % 4 == 0
        {
            verts[0] = new Vector3(0, 0, 0);
            verts[1] = new Vector3(0, 2, 0);
            verts[2] = new Vector3(1, 1, 0);
        }
        if (i == 1) //Up Triangle => mod % 4 == 1
        {
            verts[0] = new Vector3(0, 2, 0);
            verts[1] = new Vector3(2, 2, 0);
            verts[2] = new Vector3(1, 1, 0);
        }
        if (i == 2) //Right Triangle => mod % 4 == 2
        {
            verts[0] = new Vector3(1, 1, 0);
            verts[1] = new Vector3(2, 2, 0);
            verts[2] = new Vector3(2, 0, 0);
        }
        if (i == 3) //Down Triangle => mod % 4 == 3
        {
            verts[0] = new Vector3(0, 0, 0);
            verts[1] = new Vector3(1, 1, 0);
            verts[2] = new Vector3(2, 0, 0);
        }
        return verts;
    }
    #endregion
    
    #region GetNeighbours
    public static List<int> GetTriangleNeighbourList(int number,int lineTriangleCount)
    {
        //Returns number difference of all neighbours of a triangle based on modular
        
        var mod = number % 4;
        var numberDifference = new List<int>();
        if (mod == 0)
        {
            numberDifference.Add(1);
            numberDifference.Add(3);
            numberDifference.Add(-2);
        }
        if (mod == 1)
        {
            numberDifference.Add(-1);
            numberDifference.Add(1);
            numberDifference.Add(-(lineTriangleCount-2));
        }
        if (mod == 2)
        {
            numberDifference.Add(-1);
            numberDifference.Add(1);
            numberDifference.Add(2);
        }
        if (mod == 3)
        {
            numberDifference.Add(-3);
            numberDifference.Add(-1);
            numberDifference.Add(lineTriangleCount-2);
        }
        return numberDifference;
    }
    #endregion
    public static Transform GetParentTrOnCenter(MeshCollider col)
    {
        var parent = new GameObject().transform;
        parent.position = col.bounds.center;
        return parent;
    }
    public static Vector3 GetShapeStartPos()
    {
        var xPos = Random.Range(-1f, 1f);
        var yPos = Random.Range(-2f, -3f);
        return new Vector3(xPos, yPos, -0.5f);
    }
    public static Vector3 GetShapeEndPos()
    {
        return new Vector3(0, 5, 0);
    }
    public static void DotFilled(bool filled)
    {
        if (filled) _dotFilledCount++;
        else _dotFilledCount--;
        if (_dotFilledCount == DotCount)
        {
            LevelCompleted?.Invoke();
            _dotFilledCount = 0;
        }
    }
}
