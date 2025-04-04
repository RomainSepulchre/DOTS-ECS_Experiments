using NUnit.Framework;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class TestMathRandom : MonoBehaviour
{
    public int linesToDraw;

    public Transform randomCtrOrigin;
    public Transform randomInitOrigin;
    public Transform randomCreateFromIndexOrigin;

    public bool ChangeColorOverLineDraw;

    [Header("Ints controls")]
    public bool showCtrInts = false;
    public bool showInitInts = false;
    public bool showCreateIndexInts = false;

    public Vector3 intsOffset = new Vector3(-10, 0, 50);
    public float intsStep = 0.05f;

    [Header("Graph controls")]

    public bool showCtrGraph = false;
    public bool showInitGraph = false;
    public bool showCreateIndexGraph= false;

    public Vector3 graphOffset = new Vector3(-10, 0, 50);
    public float graphStep = 0.5f;

    List<int> ctrInts = new List<int>();
    List<int> initInts = new List<int>();
    List<int> createFromIndexInts = new List<int>();

    Random randomCtr1;
    Random randomCtr2;
    Random randomInit1;
    Random randomInit2;
    Random randomCreateIndex1;
    Random randomCreateIndex2;
    Random randomCreateIndex1Bis;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //randomCtr1 = new Random(1);
        //randomCtr2 = new Random(2);
        //randomInit1 = new Random();
        //randomInit1.InitState(1);
        //randomInit2 = new Random();
        //randomInit2.InitState(2);
        //randomCreateIndex1 = Random.CreateFromIndex(1);
        //randomCreateIndex2 = Random.CreateFromIndex(2);
        //randomCreateIndex1Bis = Random.CreateFromIndex(1);

        //Debug.Log($"randomCtr1({randomCtr1.state}): {randomCtr1.NextInt(0, int.MaxValue)} --- {randomCtr1.NextFloat3Direction()}");
        //Debug.Log($"randomCtr2({randomCtr2.state}): {randomCtr2.NextInt(0, int.MaxValue)} --- {randomCtr2.NextFloat3Direction()}");
        //Debug.Log($"randomInit1({randomInit1.state}): {randomInit1.NextInt(0, int.MaxValue)} --- {randomInit1.NextFloat3Direction()}");
        //Debug.Log($"randomInit2({randomInit2.state}): {randomInit2.NextInt(0, int.MaxValue)} --- {randomInit2.NextFloat3Direction()}");
        //Debug.Log($"randomCreateIndex1({randomCreateIndex1.state}): {randomCreateIndex1.NextInt(0, int.MaxValue)} --- {randomCreateIndex1.NextFloat3Direction()}");
        //Debug.Log($"randomCreateIndex2({randomCreateIndex2.state}): {randomCreateIndex2.NextInt(0, int.MaxValue)} --- {randomCreateIndex2.NextFloat3Direction()}");
        //Debug.Log($"randomCreateIndex1Bis({randomCreateIndex1Bis.state}): {randomCreateIndex1Bis.NextInt(0, int.MaxValue)} --- {randomCreateIndex1Bis.NextFloat3Direction()}");

        
    }

    // Update is called once per frame
    void Update()
    {
        ctrInts.Clear();
        initInts.Clear();
        createFromIndexInts.Clear();

        // Draw Dir
        for (int i = 1; i < linesToDraw; i++)
        {
            float3 origin = randomCtrOrigin.position;
            Random random = new Random((uint)i);
            float3 dir = random.NextFloat3Direction();

            Color col = Color.green;
            if(ChangeColorOverLineDraw)
            {
                col.a = 1f - ((i + 1) / (float)linesToDraw);
            }

            Debug.DrawLine(origin, origin + dir * 5, col);
            ctrInts.Add(random.NextInt());
        }

        for (int i = 1; i < linesToDraw; i++)
        {
            float3 origin = randomInitOrigin.position;
            Random random = new Random();
            random.InitState((uint)i);
            float3 dir = random.NextFloat3Direction();
            Debug.DrawLine(origin, origin + dir * 5, Color.cyan);
            initInts.Add(random.NextInt());
        }

        for (int i = 1; i < linesToDraw; i++)
        {
            float3 origin = randomCreateFromIndexOrigin.position;
            Random random = Random.CreateFromIndex((uint)i);
            float3 dir = random.NextFloat3Direction();
            Debug.DrawLine(origin, origin + dir * 5, Color.red);
            createFromIndexInts.Add(random.NextInt());
        }


        // Draw ints line
        if (showCtrInts)
        {
            for (int i = 0; i < ctrInts.Count; i++)
            {
                float3 origin = randomCtrOrigin.position - intsOffset;
                origin.x += i * intsStep;
                float zOffset = (ctrInts[i] / (float)int.MaxValue) * 50f;
                Debug.DrawLine(origin, origin + new float3(0, 0, zOffset), Color.green);
            } 
        }

        if (showInitInts)
        {
            for (int i = 0; i < initInts.Count; i++)
            {
                float3 origin = randomInitOrigin.position - intsOffset;
                origin.x += i * intsStep;
                float zOffset = (initInts[i] / (float)int.MaxValue) * 50f;
                Debug.DrawLine(origin, origin + new float3(0, 0, zOffset), Color.cyan);
            } 
        }

        if (showCreateIndexInts)
        {
            for (int i = 0; i < createFromIndexInts.Count; i++)
            {
                float3 origin = randomCreateFromIndexOrigin.position - intsOffset;
                origin.x += i * intsStep;
                float zOffset = (createFromIndexInts[i] / (float)int.MaxValue) * 50f;
                Debug.DrawLine(origin, origin + new float3(0, 0, zOffset), Color.red);
            } 
        }

        // Draw int graph

        if (showCtrGraph)
        {
            float3 lastPosCtr = randomCtrOrigin.position - graphOffset;
            for (int i = 0; i < initInts.Count; i++)
            {
                float zOffset = (ctrInts[i] / (float)int.MaxValue) * 50f;
                float3 newPos = randomCtrOrigin.position - graphOffset + new Vector3(0, 0, zOffset);
                newPos.x += i * graphStep;
                Debug.DrawLine(lastPosCtr, newPos, Color.green);
                lastPosCtr = newPos;
            }
        }

        if (showInitGraph)
        {
            float3 lastPosInit = randomInitOrigin.position - graphOffset;
            for (int i = 0; i < initInts.Count; i++)
            {
                float zOffset = (initInts[i] / (float)int.MaxValue) * 50f;
                float3 newPos = randomInitOrigin.position - graphOffset + new Vector3(0, 0, zOffset);
                newPos.x += i * graphStep;
                Debug.DrawLine(lastPosInit, newPos, Color.cyan);
                lastPosInit = newPos;
            } 
        }

        if (showCreateIndexGraph)
        {
            float3 lastPosCreate = randomCreateFromIndexOrigin.position - graphOffset;
            for (int i = 0; i < createFromIndexInts.Count; i++)
            {
                float zOffset = (createFromIndexInts[i] / (float)int.MaxValue) * 50f;
                float3 newPos = randomCreateFromIndexOrigin.position - graphOffset + new Vector3(0, 0, zOffset);
                newPos.x += i * graphStep;
                Debug.DrawLine(lastPosCreate, newPos, Color.red);
                lastPosCreate = newPos;
            } 
        }
    }
}
