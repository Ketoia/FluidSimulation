using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways(), SelectionBase()]
public class FluidGrid : MonoBehaviour
{
    [HideInInspector] public int gridID;

    [Range(1f, 108f)]
    public int gridResolution = 108; //108 grid cells per 1m -> 1pixel - 1 grid cell
    public GameObject gridVisual;
    public Vector2 Length;

    private float[] Velocity_Cache;
    private float[] inkBuffer_Cache;
    ComputeBuffer velocityBuffer;
    //ComputeBuffer velocityBuffer;

    private void Start()
    {

    }

    private void Update()
    {
        Vector3 newPos = (Vector3)Vector3Int.RoundToInt(transform.position * gridResolution) / gridResolution;
        gridVisual.transform.position = newPos;

        Vector3 newScale = (Vector3)Vector3Int.RoundToInt(new Vector3(Length.x, 1, Length.y)) * 0.1f;
        gridVisual.transform.localScale = newScale;
    }

    private void OnEnable()
    {
        FluidSimulation.instance.LoadGridCell(this);
    }

    private void OnDisable()
    {
        FluidSimulation.instance.UnloadGridCell(this);
    }

    public void DispatchVelocity()
    {

    }

    public Vector2Int GetCenterCoord()
    {
        return Vector2Int.RoundToInt(new Vector2(gridVisual.transform.position.x * gridResolution, gridVisual.transform.position.y * gridResolution));
    }
    
    /// <summary>
    /// Get left down corner of this grid
    /// </summary>
    /// <returns></returns>
    public Vector2Int GetLDCornerCoord()
    {
        return Vector2Int.RoundToInt(new Vector2(gridVisual.transform.position.x * gridResolution - Length.x * 0.5f * gridResolution, gridVisual.transform.position.y * gridResolution - Length.y * 0.5f * gridResolution));
    }

    /// <summary>
    /// Get right top corner of this grid
    /// </summary>
    /// <returns></returns>
    public Vector2Int GetRTCornerCoord()
    {
        return Vector2Int.RoundToInt(new Vector2(gridVisual.transform.position.x * gridResolution + Length.x * 0.5f * gridResolution, gridVisual.transform.position.y * gridResolution + Length.y * 0.5f * gridResolution));
    }

}
