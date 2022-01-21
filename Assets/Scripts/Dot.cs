using System.Collections;
using UnityEngine;

public class Dot : MonoBehaviour
{
    /// <summary>
    /// Dots are checking to see if they are filled. If all filled, level is completed.
    /// </summary>
    
    private MeshRenderer _renderer;
    private bool _filled;
    private bool _lastSituation;
    [SerializeField] private LayerMask shapeLayer;
    private void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
        Helper.DotCount++;
        Helper.LevelCompleted += LevelCompleted;
    }
    private void OnDisable()
    {
        Helper.LevelCompleted -= LevelCompleted;
    }

    private IEnumerator CheckIfFilled()
    {
        //Waiting for physics to calculate triggers
        yield return new WaitForFixedUpdate();
        if(_lastSituation != _filled)
        {
            Helper.DotFilled(_filled);
            _lastSituation = _filled;
        }
    }
    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Physics.SyncTransforms();
            CastRay();
            StartCoroutine(CheckIfFilled());
        }
    }
    private void CastRay()
    {
        //Cast ray to check if dot is filled
        if (Physics.Raycast(transform.position-Vector3.forward*3, Vector3.forward,5,shapeLayer))
        {
            _filled = true;
            _renderer.material.color = Color.white;
        }
        else
        {
            _filled = false;
            _renderer.material.color = Color.grey;
        }
    }
    private void LevelCompleted()
    {
        _filled = false;
        _lastSituation = false;
    }
}
