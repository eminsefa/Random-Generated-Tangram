using System.Threading.Tasks;
using UnityEngine;

public class Shape : MonoBehaviour
{
    /// <summary>
    /// This script is controlling shape movements, and grid positions
    /// </summary>
    
    private Camera _mainCam;
    private MeshCollider _collider;
    private Transform _parent;
    private Vector3 _movePos;
    private Vector3 _screenPos;
    private Vector3 _offset;
    private bool _selected;
    private bool _onStartMove;
    private bool _onEndMove;
    private async void Start()
    {
        _mainCam=Camera.main;
        _collider=gameObject.AddComponent<MeshCollider>();
        
        //To move easier
        _parent = Helper.GetParentTrOnCenter(_collider);
        transform.SetParent(_parent);
        
        _collider.enabled = false;
        gameObject.layer = 7;
        
        Helper.LevelCompleted += LevelCompleted;
        
        _movePos = Helper.GetShapeStartPos();
        await Task.Delay(200);
        _onStartMove = true;
    }
    private void OnDisable()
    {
        Helper.LevelCompleted -= LevelCompleted;
    }
    private async void LevelCompleted()
    {
        _selected = false;
        _collider.enabled = false;
        
        await Task.Delay(100);
        _parent = Helper.GetParentTrOnCenter(_collider);
        transform.SetParent(_parent);
        _onEndMove = true;
        _movePos = Helper.GetShapeEndPos();
    }
    private void Update()
    {
        //Start and end moves. Would be better to use DoTween.
        
        if (_onStartMove || _onEndMove)
        {
            _parent.position = Vector3.MoveTowards(_parent.position, _movePos, 20 * Time.deltaTime);
            if (Vector3.Distance(_parent.position, _movePos) < 0.1f)
            {
                if (_onStartMove)
                {
                    ShapeMovedToStartPos();
                }
                if(_onEndMove)
                {
                    Destroy(_parent.gameObject);
                    Destroy(gameObject);
                }
            }
        }
    }
    private void ShapeMovedToStartPos()
    {
        _onStartMove = false;
        transform.SetParent(null);
        _collider.enabled = true;
        Destroy(_parent.gameObject);
    }
    private void OnMouseDrag()
    {
        if(!_selected) return;
        var mousePos = Input.mousePosition;
        var cursorScreenPos = new Vector3(mousePos.x, mousePos.y, _screenPos.z);
        var cursorWorldPos = _mainCam.ScreenToWorldPoint(cursorScreenPos) + _offset;
        transform.position = cursorWorldPos;
    }
    private void OnMouseDown()
    {
        if(_onEndMove) return;
        Selected(true);
        var mousePos = Input.mousePosition;
        var objPos = transform.position;
        _screenPos = _mainCam.WorldToScreenPoint(objPos);
        _offset = objPos - _mainCam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, _screenPos.z));
    }
    private void OnMouseUp()
    {
        Selected(false);
        FitToGrid();
    }
    private void Selected(bool selected)
    {
        //For render ordering
        
        var z = -0.1f;
        if (selected) _selected = true;
        else
        {
            _selected = false;
            z = 0.1f;
        }
        var pos = transform.position;
        pos.z += z;
        transform.position = pos;
    }
    private void FitToGrid()
    {
        //Fits position to grids
        
        var pos = transform.position;
        var halfGridAxisSize = Helper.HalfGridAxisSize;
        var endX = Mathf.Round(pos.x*2)*halfGridAxisSize;
        var endY = Mathf.Round(pos.y*2)*halfGridAxisSize;
        pos.x = endX;
        pos.y = endY;
        transform.position = pos;
    }
}
