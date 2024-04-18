using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class FluidSimulation : MonoBehaviour
{
    private static FluidSimulation _instance;
    public static FluidSimulation instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<FluidSimulation>();
            return _instance;
        }
    }

    public int size = 64;
    [Tooltip("pixel to cell simulation")]
    public float ratioGameToSimulation = 2;
    private int scaledSize;
    public RawImage testImage;
    public ComputeShader fluidComputeShader;
    public Color color;
    public SpriteRenderer outSprite;
    Texture2D testTexture;
    public Material RenderMaterial;

    private ComputeBuffer velocity;
    private ComputeBuffer velocity_Temp;
    private ComputeBuffer density;
    private ComputeBuffer density_Temp;
    private ComputeBuffer divergence;
    private ComputeBuffer vorticity; //For later
    private ComputeBuffer BoundaryIndexes;
    private ComputeBuffer inkBuffer;

    [SerializeField] private RenderTexture displayTexture;

    //Kernels id
    int KernelID_Advect;
    int KernelID_Diffuse;
    int KernelID_AddExternalForces;
    int KernelID_SubtractPressureGradient;

    int KernelID_SwapDensity;
    int KernelID_SwapVelocity;
    int KernelID_AddForcesHardCoded;

    int KernelID_Divergence;

    int KernelID_CopyDensityToRT;

    int KernelID_BoundaryVelocity;
    int KernelID_BoundaryPressure;
    int KernelID_BoundaryInk;
    //

    public float virtualGridCellSize = 1;
    public GameObject gridTemplate;

    //TODO: Generic grid, scale, Generic functions
    //TODO: Grid Connecting, Referencing to other grids (maybe overkill but idk), Grid Optimalization, Check on steamdeck

    private void Awake()
    {
        //instance = this;
    }

    void Start()
    {
        //rt = new RenderTexture(size, size, 0);
        //rt.enableRandomWrite = true;

        //testImage.texture = rt;
        Init();
        GameObject newObj = Instantiate(gridTemplate, Vector3.zero, Quaternion.Euler(90, 0, 180));
        newObj.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", displayTexture);
        newObj.transform.localScale = new Vector3(virtualGridCellSize * 0.1f, 1, virtualGridCellSize * 0.1f);

        //--------------------  ADVECT  -----------------------------

        fluidComputeShader.SetBuffer(KernelID_Advect, "u", velocity);
        fluidComputeShader.SetBuffer(KernelID_Advect, "u_temp", velocity_Temp);
        fluidComputeShader.SetBuffer(KernelID_Advect, "ink", inkBuffer);

        //--------------------  DIFUSE  -----------------------------

        fluidComputeShader.SetBuffer(KernelID_Diffuse, "u", velocity);
        fluidComputeShader.SetBuffer(KernelID_Diffuse, "p", density);
        fluidComputeShader.SetBuffer(KernelID_Diffuse, "u_temp", velocity_Temp);
        fluidComputeShader.SetBuffer(KernelID_Diffuse, "p_temp", density_Temp);
        fluidComputeShader.SetBuffer(KernelID_Diffuse, "div", divergence);

        //--------------------  COMPUTE DIVERGENCE  -----------------------------

        fluidComputeShader.SetBuffer(KernelID_Divergence, "u", velocity);
        fluidComputeShader.SetBuffer(KernelID_Divergence, "div", divergence);

        //--------------------  SUBSTRACT DENSITY  -----------------------------

        fluidComputeShader.SetBuffer(KernelID_SubtractPressureGradient, "u", velocity);
        fluidComputeShader.SetBuffer(KernelID_SubtractPressureGradient, "u_temp", velocity_Temp);
        fluidComputeShader.SetBuffer(KernelID_SubtractPressureGradient, "p", density);
        fluidComputeShader.SetBuffer(KernelID_SubtractPressureGradient, "p_temp", density_Temp);

        //--------------------  COPY TO RENDER TEXTURE  -----------------------------

        fluidComputeShader.SetTexture(KernelID_CopyDensityToRT, "FuildRT", displayTexture);
        fluidComputeShader.SetBuffer(KernelID_CopyDensityToRT, "ink", inkBuffer);
        fluidComputeShader.SetBuffer(KernelID_CopyDensityToRT, "p_temp", density);
        fluidComputeShader.SetBuffer(KernelID_CopyDensityToRT, "div", divergence);

        //--------------------  BOUNDARIES  -----------------------------

        fluidComputeShader.SetBuffer(KernelID_BoundaryVelocity, "u", velocity);
        fluidComputeShader.SetBuffer(KernelID_BoundaryVelocity, "boundaryIndexes", BoundaryIndexes);
        fluidComputeShader.SetBuffer(KernelID_BoundaryPressure, "p", density);
        fluidComputeShader.SetBuffer(KernelID_BoundaryPressure, "boundaryIndexes", BoundaryIndexes);
        fluidComputeShader.SetBuffer(KernelID_BoundaryInk, "ink", inkBuffer);
        fluidComputeShader.SetBuffer(KernelID_BoundaryInk, "boundaryIndexes", BoundaryIndexes);

        fluidComputeShader.SetBuffer(KernelID_Advect, "boundaryIndexes", BoundaryIndexes);
        fluidComputeShader.SetBuffer(KernelID_BoundaryPressure, "boundaryIndexes", BoundaryIndexes);
        fluidComputeShader.SetBuffer(KernelID_Diffuse, "boundaryIndexes", BoundaryIndexes);
        fluidComputeShader.SetBuffer(KernelID_SubtractPressureGradient, "boundaryIndexes", BoundaryIndexes);

        //--------------------  FORCES  -----------------------------

        fluidComputeShader.SetBuffer(KernelID_AddExternalForces, "u", velocity);
        fluidComputeShader.SetBuffer(KernelID_AddExternalForces, "div", divergence);
        fluidComputeShader.SetBuffer(KernelID_AddExternalForces, "u_temp", velocity_Temp);

        fluidComputeShader.SetBuffer(KernelID_AddForcesHardCoded, "u", velocity);
        fluidComputeShader.SetBuffer(KernelID_AddForcesHardCoded, "u_temp", velocity_Temp);
        fluidComputeShader.SetBuffer(KernelID_AddForcesHardCoded, "ink", inkBuffer);

        //--------------------  ADDITIONAL  -----------------------------

        fluidComputeShader.SetBuffer(KernelID_SwapVelocity, "u", velocity);
        fluidComputeShader.SetBuffer(KernelID_SwapVelocity, "u_temp", velocity_Temp);

        fluidComputeShader.SetBuffer(KernelID_SwapDensity, "p", density);
        fluidComputeShader.SetBuffer(KernelID_SwapDensity, "p_temp", density_Temp);

        fluidComputeShader.SetFloat("visc", 2);
        fluidComputeShader.SetFloat("size", size);
        fluidComputeShader.SetFloat("rdx", 1);

    }

    void Update()
    {
        UpdateSimulation();
    }

    private void Init()
    {
        FindKernels();

        //
        displayTexture = new RenderTexture(size, size, 0);
        testImage.GetComponent<RectTransform>().sizeDelta = new Vector2(scaledSize, scaledSize);
        displayTexture.enableRandomWrite = true;
        testImage.texture = displayTexture;

        //testTexture = new Texture2D(size, size);
        //testTexture.Apply();
        //outSprite.sprite = Sprite.Create(testTexture, new Rect(0, 0, size, size), Vector2.one * 0.5f);
        //RenderMaterial.SetTexture("_MainTex", displayTexture);

        int count = size * size;
        velocity = new ComputeBuffer(count, sizeof(float) * 2);
        velocity_Temp = new ComputeBuffer(count, sizeof(float) * 2);
        density = new ComputeBuffer(count, sizeof(float));
        density_Temp = new ComputeBuffer(count, sizeof(float));

        divergence = new ComputeBuffer(count, sizeof(float));
        BoundaryIndexes = new ComputeBuffer(count, sizeof(int));

        //Data to set
        inkBuffer = new ComputeBuffer(count, sizeof(float));

        //For later
        //Vorticity = new ComputeBuffer(count, sizeof(float));


        //Default data
        float[] defaultData = new float[count];
        Array.Fill(defaultData, 0);
        Vector2[] defaultData2 = new Vector2[count];
        Array.Fill(defaultData2, Vector2.zero);

        velocity.SetData(defaultData2);
        density.SetData(defaultData);
        divergence.SetData(defaultData);
        inkBuffer.SetData(defaultData);
        //Vorticity.SetData(defaultData);

        BakeIndexes();

        fluidComputeShader.SetVector("Color", color);
    }

    private void FindKernels()
    {

        KernelID_Advect = fluidComputeShader.FindKernel("Advect");
        KernelID_Diffuse = fluidComputeShader.FindKernel("Diffuse");
        KernelID_AddExternalForces = fluidComputeShader.FindKernel("AddExternalForces");
        KernelID_SubtractPressureGradient = fluidComputeShader.FindKernel("SubtractPressureGradient");

        KernelID_SwapDensity = fluidComputeShader.FindKernel("SwapDensity");
        KernelID_SwapVelocity = fluidComputeShader.FindKernel("SwapVelocity");

        KernelID_CopyDensityToRT = fluidComputeShader.FindKernel("CopyDensityToRT");
        KernelID_AddForcesHardCoded = fluidComputeShader.FindKernel("AddForcesHardCoded");
        KernelID_Divergence = fluidComputeShader.FindKernel("Divergence");

        KernelID_BoundaryVelocity = fluidComputeShader.FindKernel("BoundaryVelocity");
        KernelID_BoundaryPressure = fluidComputeShader.FindKernel("BoundaryPressure");
        KernelID_BoundaryInk = fluidComputeShader.FindKernel("BoundaryInk");
    }

    private void UpdateSimulation()
    {
        //---------------
        // 1.  Advect
        //---------------
        // Advect velocity (velocity advects itself, resulting in a divergent
        // velocity field.  Later, correct this divergence).

        // Set the no-slip velocity...
        // This sets the scale to -1, so that v[0, j] = -v[1, j], so that at 
        // the boundary between them, the avg. velocity is zero. 

        //Boudaries
        //_boundaries.SetTextureParameter("x", _iTextures[TEXTURE_VELOCITY]);
        //_boundaries.SetFragmentParameter1f("scale", -1);
        //_boundaries.Compute();

        //Later
        // Set the no-slip velocity on arbitrary interior boundaries if they are enabled
        //if (_bArbitraryBC)
        //    _arbitraryVelocityBC.Compute();
        fluidComputeShader.SetFloat("dt", Time.deltaTime);
        //fluidComputeShader.Dispatch(KernelID_AddExternalForces, size / 8, 1, 1);

        fluidComputeShader.Dispatch(KernelID_BoundaryVelocity, size / 8, size / 8, 1);
        fluidComputeShader.Dispatch(KernelID_SwapVelocity, size / 8, size / 8, 1);


        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    float x = UnityEngine.Random.Range(0.0f, 1.0f);
        //    float y = UnityEngine.Random.Range(0.0f, 1.0f);
        //    float z = UnityEngine.Random.Range(0.0f, 1.0f);
        //    fluidComputeShader.SetVector("Color", new Vector4(x, y, z, 1));
        //}        


        if (Input.GetKey(KeyCode.Space))
        {
            //fluidComputeShader.Dispatch(KernelID_AddForcesHardCoded, size / (8 * 8), size / (8 * 8), 1);
            fluidComputeShader.Dispatch(KernelID_AddForcesHardCoded, size / (8 * 4), size / (8 * 2), 1);
        }


        //if (Input.GetKey(KeyCode.D))
        {
            //fluidComputeShader.Dispatch(KernelID_AddForcesHardCoded, size / (8 * 8), size / (8 * 8), 1);
        }

        // Advect velocity.

        fluidComputeShader.SetInt("dissipation", 1);
        fluidComputeShader.Dispatch(KernelID_Advect, size / 8, size / 8, 1);
        fluidComputeShader.Dispatch(KernelID_SwapVelocity, size / 8, size / 8, 1);

        fluidComputeShader.Dispatch(KernelID_BoundaryVelocity, size / 8, size / 8, 1);
        fluidComputeShader.Dispatch(KernelID_SwapVelocity, size / 8, size / 8, 1);

        fluidComputeShader.Dispatch(KernelID_BoundaryInk, size / 8, size / 8, 1);

        // Set ink boundaries to zero
        //_boundaries.SetTextureParameter("x", _iTextures[TEXTURE_DENSITY]);
        //_boundaries.SetFragmentParameter1f("scale", 0);
        //_boundaries.Compute();

        // Advect "ink", a passive scalar carried by the flow.
        //_advect.SetFragmentParameter1f("dissipation", _rInkLongevity);
        //_advect.SetOutputTexture(_iTextures[TEXTURE_DENSITY], _iWidth, _iHeight);
        //_advect.SetTextureParameter("x", _iTextures[TEXTURE_DENSITY]);
        //_advect.Compute();


        //Vorticity

        //Diffuse if Vorticity > 0 //For later
        //fluidComputeShader.Dispatch(KernelID_CopyDensityToRT, size / 8, size / 8, 1);
        //return;

        fluidComputeShader.Dispatch(KernelID_Divergence, size / 8, size / 8, 1);
        fluidComputeShader.Dispatch(KernelID_AddExternalForces, size / (8), size / 8, 1);
        fluidComputeShader.Dispatch(KernelID_BoundaryVelocity, size / 8, size / 8, 1);

        for (int i = 0; i < 20; i++)
        {
            fluidComputeShader.Dispatch(KernelID_BoundaryPressure, size / 8, size / 8, 1);
            fluidComputeShader.Dispatch(KernelID_SwapDensity, size / 8, size / 8, 1);

            fluidComputeShader.Dispatch(KernelID_Diffuse, size / 8, size / 8, 1);
            fluidComputeShader.Dispatch(KernelID_SwapDensity, size / 8, size / 8, 1);
        }

        fluidComputeShader.Dispatch(KernelID_SubtractPressureGradient, size / 8, size / 8, 1);
        fluidComputeShader.Dispatch(KernelID_SwapVelocity, size / 8, size / 8, 1);
        fluidComputeShader.Dispatch(KernelID_BoundaryVelocity, size / 8, size / 8, 1);
        fluidComputeShader.Dispatch(KernelID_SwapVelocity, size / 8, size / 8, 1);

        fluidComputeShader.Dispatch(KernelID_CopyDensityToRT, size / 8, size / 8, 1);

        //Graphics.DrawTexture(new Rect(250, 250, scaledSidisplayTextureze, scaledSize), displayTexture);
        //Graphics.DrawTexture(new Rect(10, 10, 100, 100), displayTexture);
        //Graphics.DrawTexture(Camera.main.rect, displayTexture);


    }

    private void OnDestroy()
    {


        displayTexture.Release();

    }

    private void BakeIndexes() //Propably move it to the 
    {
        int[] indexes = new int[size * size];
        bool[] isInCollider = new bool[size * size];

        for (int i = 0; i < size * size; i++) //fill default values
        {
            indexes[i] = -1;
        }

        //First boundary indexes
        for (int i = 0; i < size; i++) // down
        {
            indexes[i] = i + size;
        }

        for (int i = 0; i < size; i++) // up
        {
            indexes[i + (size - 1) * size] = i + (size - 2) * size;
        }

        for (int i = 0; i < size; i++) // left
        {
            indexes[i * size] = i * size + 1;
        }

        for (int i = 0; i < size; i++) // right
        {
            indexes[i * size + size - 1] = i * size + size - 2;
        }

        Vector2Int centerIndex = new Vector2Int(size / 2, size / 2 - 16);
        float r = 16;
        for (int i = -(int)r; i < r; i++)
        {
            for (int j = -(int)r; j < r; j++)
            {
                float distance = Vector2.Distance(Vector2.zero, new Vector2(i, j));

                if (distance < r)
                {
                    Vector2Int newIndex = centerIndex + new Vector2Int(i, j);
                    indexes[newIndex.x + newIndex.y * size] = -2;
                }
            }
        }

        for (int i = -(int)r - 2; i < r + 2; i++)
        {
            for (int j = -(int)r - 2; j < r + 2; j++)
            {
                Vector2Int newIndex = centerIndex + new Vector2Int(i, j);

                Vector2Int IndexU = centerIndex + new Vector2Int(i, j + 1);
                Vector2Int IndexD = centerIndex + new Vector2Int(i, j - 1);
                Vector2Int IndexR = centerIndex + new Vector2Int(i + 1, j);
                Vector2Int IndexL = centerIndex + new Vector2Int(i - 1, j);

                Vector2Int IndexUL = centerIndex + new Vector2Int(i + 1, j - 1);
                Vector2Int IndexUR = centerIndex + new Vector2Int(i + 1, j + 1);
                Vector2Int IndexDL = centerIndex + new Vector2Int(i - 1, j - 1);
                Vector2Int IndexDR = centerIndex + new Vector2Int(i - 1, j + 1);

                int value0 = indexes[newIndex.x + newIndex.y * size];
                int valueU = indexes[IndexU.x + IndexU.y * size];
                int valueD = indexes[IndexD.x + IndexD.y * size];
                int valueR = indexes[IndexR.x + IndexR.y * size];
                int valueL = indexes[IndexL.x + IndexL.y * size];

                int valueUL = indexes[IndexUL.x + IndexUL.y * size];
                int valueUR = indexes[IndexUR.x + IndexUR.y * size];
                int valueDL = indexes[IndexDL.x + IndexDL.y * size];
                int valueDR = indexes[IndexDR.x + IndexDR.y * size];

                if (value0 != -2 && (valueU == -2 || valueR == -2 || valueL == -2 || valueD == -2 || valueUL == -2 || valueUR == -2 || valueDL == -2 || valueDR == -2))
                {
                    if (valueU == -2)
                        indexes[newIndex.x + newIndex.y * size] = IndexD.x + IndexD.y * size;
                    else if (valueD == -2)
                        indexes[newIndex.x + newIndex.y * size] = IndexU.x + IndexU.y * size;
                    else if (valueR == -2)
                        indexes[newIndex.x + newIndex.y * size] = IndexL.x + IndexL.y * size;
                    else if (valueL == -2)
                        indexes[newIndex.x + newIndex.y * size] = IndexR.x + IndexR.y * size;
                    else if (valueUL == -2)
                        indexes[newIndex.x + newIndex.y * size] = IndexDR.x + IndexDR.y * size;
                    else if (valueUR == -2)
                        indexes[newIndex.x + newIndex.y * size] = IndexDL.x + IndexDL.y * size;
                    else if (valueDL == -2)
                        indexes[newIndex.x + newIndex.y * size] = IndexUR.x + IndexUR.y * size;
                    else if (valueDR == -2)
                        indexes[newIndex.x + newIndex.y * size] = IndexUL.x + IndexUL.y * size;
                }
            }
        }

        BoundaryIndexes.SetData(indexes);
    }

    public void LoadGridCell(FluidGrid fluidGrid)
    {

    }

    public void UnloadGridCell(FluidGrid fluidGrid)
    {

    }
}