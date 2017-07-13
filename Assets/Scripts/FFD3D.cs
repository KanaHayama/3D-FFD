using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System;

public class FFD3D : MonoBehaviour {
    private const string CONTROL_POINT_PATH = "Prefebs/ControlPoint";
    private const string CONNECTOR_PATH = "Prefebs/Connector";
    private const float CONNECTOR_WIDTH = 0.01f;
    private const string CONPUTE_SHADER_PATH = "ComputeShader/ffd3d";
    private const string VERTEX_KERNEL_NAME = "VertexMain";
    private const string NORMAL_KERNEL_NAME = "NormalMain";
    private const int THREAD_GROUPS_X = 32;
    private const string CONTROL_POINT_NUMBER_NAME = "controlPointNumbers";
    private const string X_BERNSTEIN_POLYNOMIAL_BUFFER_NAME = "xBernsteinPolynomials";
    private const string Y_BERNSTEIN_POLYNOMIAL_BUFFER_NAME = "yBernsteinPolynomials";
    private const string Z_BERNSTEIN_POLYNOMIAL_BUFFER_NAME = "zBernsteinPolynomials";
    private const string CONTROL_POINT_BUFFER_NAME = "controlPoints";
    private const string OUTPUT_VERTEX_BUFFER_NAME = "outputVertices";
    private const string VERTEX_BUFFER_NAME = "vertices";
    private const string TRIANGLE_BUFFER_NAME = "triangles";
    private const string OFFSET_BUFFER_NAME = "offsets";
    private const string SIZE_BUFFER_NAME = "sizes";
    private const string OUTPUT_NORMAL_BUFFER_NAME = "outputNormals";

    //用于记录一个三角形三个顶点的编号
    private struct Triangle
    {
        public int i0;
        public int i1;
        public int i2;

        public Triangle(int i0, int i1, int i2)
        {
            this.i0 = i0;
            this.i1 = i1;
            this.i2 = i2;
        }

        public bool isIn(int index)
        {
            return index == i0 || index == i1 || index == i2;
        }
    }

    private int xControlPointNumber;
    private int yControlPointNumber;
    private int zControlPointNumber;
    private List<Vector3> vertexCoordinates;
    private Triangle[] adjacentTriangles;
    private int[] adjacentTriangleSizes;
    private int[] adjacentTriangleOffsets;
    private List<GameObject> controlPoints;
    private List<GameObject> xConnectors;
    private List<GameObject> yConnectors;
    private List<GameObject> zConnectors;
    private Mesh mesh;
    private Vector3 minMeshCoordinate;
    private float xSize;
    private float ySize;
    private float zSize;
    float[] xBernsteinPolynomials;
    float[] yBernsteinPolynomials;
    float[] zBernsteinPolynomials;
    private bool isShowObject;
    private bool isShowControlPoints;
    private bool isShowConnectors;
    private bool isModified = false;
    private Vector3[] lastControlPointPositions;
    private int vertexKernelId;
    private int normalKernelId;
    private bool isCPUMode = true;

    public ComputeShader computeShader;
    public GameObject controlPoint;
    public GameObject connector;
    public bool showObject
    {
        get { return isShowObject; }
        set { gameObject.GetComponent<MeshRenderer>().enabled = value; isShowObject = value; }
    }
    public bool showControlPoints
    {
        get { return isShowControlPoints; }
        set { if (controlPoints != null) foreach (GameObject o in controlPoints) o.GetComponent<MeshRenderer>().enabled = value; isShowControlPoints = value; }
    }
    public bool showConnectors
    {
        get { return isShowConnectors; }
        set { List<GameObject>[] lists = { xConnectors, yConnectors, zConnectors }; foreach (List<GameObject> list in lists) if (list != null) foreach (GameObject o in list) o.GetComponent<LineRenderer>().enabled = value; isShowConnectors = value; }
    }
    public bool modified { get { return isModified; } }
    public int xNumber { get { return xControlPointNumber; } }
    public int yNumber { get { return yControlPointNumber; } }
    public int zNumber { get { return zControlPointNumber; } }

    public delegate void reloadDelegate();
    public event reloadDelegate reloadEvent;

    private void Awake()
    {
        if (controlPoint == null)
        {
            controlPoint = Resources.Load(CONTROL_POINT_PATH) as GameObject;//加载控制点预制体
        }
        if (connector == null)
        {
            connector = Resources.Load(CONNECTOR_PATH) as GameObject;//加载连接线预制体
        }
        if (computeShader == null)
        {
            computeShader = Resources.Load(CONPUTE_SHADER_PATH) as ComputeShader;//加载ComputeShader
        }
    }

    private void Start ()
    {
        if (SystemInfo.supportsComputeShaders)
        {
            vertexKernelId = computeShader.FindKernel(VERTEX_KERNEL_NAME);//获得ComputeShader中计算顶点坐标的函数Id
            normalKernelId = computeShader.FindKernel(NORMAL_KERNEL_NAME);//获得ComputeShader中计算顶点法向量的函数Id
        }
        else
        {
            computeShader = null;//禁止切换到GPU模式
        }
        initMesh();
        initControlPoints(3, 3, 3);
    }
    
    //每一帧更新
    private void LateUpdate ()
    {
        List<Vector3> p = new List<Vector3>();
        foreach (GameObject o in controlPoints) p.Add(o.transform.localPosition);
        Vector3[] currentControlPointPositons = p.ToArray();
        if (lastControlPointPositions == null || !arrayEqual(lastControlPointPositions, currentControlPointPositons))
        {            
            mesh.vertices = calculateVertices();//重新设定顶点坐标
            mesh.normals = calculateNormals(mesh.vertices);//重新设定顶点法线方向
            moveConnectors();
            isModified = true;
        }
        lastControlPointPositions = currentControlPointPositons;               
    }

    //比较两个数组是否相等
    private bool arrayEqual<T>(T[] a, T[] b)
    {
        if (a == b) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; ++i)
        {
            if (!a[i].Equals(b[i])) return false;
        }
        return true;
    }

    //与网格相关的初始化
    private void initMesh()
    {
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        vertexCoordinates = new List<Vector3>();
        //记录网格中每个顶点的相对坐标
        float xMin = float.PositiveInfinity;
        float xMax = float.NegativeInfinity;
        float yMin = float.PositiveInfinity;
        float yMax = float.NegativeInfinity;
        float zMin = float.PositiveInfinity;
        float zMax = float.NegativeInfinity;
        Vector3[] coordinates = new Vector3[mesh.vertices.Length];
        for (int i = 0; i < mesh.vertexCount; ++i)
        {
            Vector3 c = mesh.vertices[i];
            coordinates[i] = c;
            if (c.x < xMin) xMin = c.x;
            if (c.x > xMax) xMax = c.x;
            if (c.y < yMin) yMin = c.y;
            if (c.y > yMax) yMax = c.y;
            if (c.z < zMin) zMin = c.z;
            if (c.z > zMax) zMax = c.z;
        }
        Vector3 center = new Vector3((xMin + xMax) / 2, (yMin + yMax) / 2, (zMin + zMax) / 2);
        for (int i = 0; i < mesh.vertexCount; ++i)
        {
            coordinates[i] -= center;
        }
        mesh.vertices = coordinates;
        //transform.localPosition -= center;

        xSize = mesh.bounds.size.x;
        ySize = mesh.bounds.size.y;
        zSize = mesh.bounds.size.z;

        Vector3 half = new Vector3(xSize / 2, ySize / 2, zSize / 2);
        Vector3 min = -half;
        Vector3 max = half;
        minMeshCoordinate = mesh.bounds.min;

        for (int i = 0; i < mesh.vertexCount; ++i)
        {
            float xCoordinate = (mesh.vertices[i].x - min.x) / (max.x - min.x);
            float yCoordinate = (mesh.vertices[i].y - min.y) / (max.y - min.y);
            float zCoordinate = (mesh.vertices[i].z - min.z) / (max.z - min.z);
            vertexCoordinates.Add(new Vector3(xCoordinate, yCoordinate, zCoordinate));
        }

        //记录网格中每个顶点对应的所有三角形
        List<Triangle> triangles = new List<Triangle>();
        adjacentTriangleSizes = new int[mesh.vertexCount];
        int[] triangleIndices = mesh.triangles;
        for (int vertexIndex = 0; vertexIndex < mesh.vertexCount; ++vertexIndex)
        {
            List<Triangle> list = new List<Triangle>();
            for (int i = 0; i < triangleIndices.Length; i += 3)
            {
                Triangle t = new Triangle(triangleIndices[i], triangleIndices[i + 1], triangleIndices[i + 2]);
                if (t.isIn(vertexIndex))
                {
                    list.Add(t);
                }
            }
            triangles.AddRange(list);
            adjacentTriangleSizes[vertexIndex] = list.Count;
        }
        adjacentTriangles = triangles.ToArray();
        
        adjacentTriangleOffsets = new int[mesh.vertexCount];
        int offset = 0;
        for (int i = 0; i < mesh.vertexCount; ++i)
        {
            adjacentTriangleOffsets[i] = offset;
            offset += adjacentTriangleSizes[i];
        }
    }

    //初始化/重新设置控制点
    public void initControlPoints(int xNumber, int yNumber, int zNumber)
    {
        if (controlPoints != null)
        {
            deleteControlPoints();
        }
        setControlPointNumbers(xNumber, yNumber, zNumber);       
        calculateBernsteinPolynomialArrays();        
        createControlPoints();
        moveConnectors();
        isModified = false;
        lastControlPointPositions = null;
    }

    //删除控制点和连接线
    private void deleteControlPoints()
    {
        List<GameObject>[] lists = { controlPoints, xConnectors, yConnectors, zConnectors };
        foreach (List<GameObject> list in lists){
            if (list != null)
            {
                foreach (GameObject o in list)
                {
                    Destroy(o);
                }
                list.Clear();
            }            
        }
    }

    private void setControlPointNumbers(int xNumber, int yNumber, int zNumber)
    {
        xControlPointNumber = xNumber;
        yControlPointNumber = yNumber;
        zControlPointNumber = zNumber;
    }

    //阶乘
    private long factorial(int n)
    {
        long result = 1;
        for (int i = 2; i <= n; ++i)
        {
            result *= i;
        }
        return result;
    }

    //组合
    private long combination(int n, int r)
    {
        return factorial(n) / (factorial(r) * factorial(n - r));
    }
    
    private float bernsteinPolynomial(int n, int r, float s)
    {
        return (float)(combination(n, r) * System.Math.Pow(s, r) * System.Math.Pow(1.0f - s, n - r));
    }
    
    private float[] bernsteinPolynomialArray(int n, float s)
    {
        float[] result = new float[n];
        for (int r = 0; r < n; ++r)
        {
            result[r] = bernsteinPolynomial(n - 1, r, s);
        }
        return result;
    }

    //计算并保存Bernstein多项式
    private void calculateBernsteinPolynomialArrays()
    {
        List<float> xList = new List<float>();
        List<float> yList = new List<float>();
        List<float> zList = new List<float>();
        for (int i = 0; i < mesh.vertexCount; ++i)
        {
            Vector3 meshCoordinate = vertexCoordinates[i];
            xList.AddRange(bernsteinPolynomialArray(xControlPointNumber, meshCoordinate.x));
            yList.AddRange(bernsteinPolynomialArray(yControlPointNumber, meshCoordinate.y));
            zList.AddRange(bernsteinPolynomialArray(zControlPointNumber, meshCoordinate.z));
        }
        xBernsteinPolynomials = xList.ToArray();
        yBernsteinPolynomials = yList.ToArray();
        zBernsteinPolynomials = zList.ToArray();
    }

    //初始化连接线
    private void initLineRenderer(LineRenderer lineRenderer)
    {
        lineRenderer.enabled = isShowConnectors;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = CONNECTOR_WIDTH;
        lineRenderer.endWidth = CONNECTOR_WIDTH;
    }

    //设置连接线两端的坐标
    private void setLineRendererPosition(LineRenderer lineRenderer, Vector3 from, Vector3 to)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = CONNECTOR_WIDTH;
        lineRenderer.endWidth = CONNECTOR_WIDTH;
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);
    }

    private int getControlPointIndex(int x, int y, int z)
    {
        return x * (yControlPointNumber * zControlPointNumber) + y * zControlPointNumber + z;
    }

    //创建控制点与连接线
    private void createControlPoints()
    {
        //创建控制点
        controlPoints = new List<GameObject>();
        for (int x = 0; x < xControlPointNumber; ++x)
        {
            float xPercent = (float)x / (xControlPointNumber - 1);
            for (int y = 0; y < yControlPointNumber; ++y)
            {
                float yPercent = (float)y / (yControlPointNumber - 1);
                for (int z = 0; z < zControlPointNumber; ++z)
                {
                    float zPercent = (float)z / (zControlPointNumber - 1);
                    GameObject o = Instantiate(controlPoint, transform.position, Quaternion.identity) as GameObject;
                    o.transform.parent = transform;                                                            
                    o.transform.localPosition = (minMeshCoordinate + new Vector3(xPercent * xSize, yPercent * ySize, zPercent * zSize));
                    o.GetComponent<MeshRenderer>().enabled = isShowControlPoints;
                    controlPoints.Add(o);
                }
            }
        }
        //创建X方向连接线
        xConnectors = new List<GameObject>();
        for (int i = 0; i < xControlPointNumber - 1; ++i)
        {
            for (int j = 0; j < yControlPointNumber; ++j)
            {
                for (int k = 0; k < zControlPointNumber; ++k)
                {
                    GameObject c = Instantiate(connector, transform.position, Quaternion.identity) as GameObject;
                    c.transform.parent = transform;
                    LineRenderer lr = c.GetComponent<LineRenderer>();
                    initLineRenderer(lr);
                    xConnectors.Add(c);
                }
            }
        }
        //创建Y方向连接线
        yConnectors = new List<GameObject>();
        for (int i = 0; i < xControlPointNumber; ++i)
        {
            for (int j = 0; j < yControlPointNumber - 1; ++j)
            {
                for (int k = 0; k < zControlPointNumber; ++k)
                {
                    GameObject c = Instantiate(connector, transform.position, Quaternion.identity) as GameObject;
                    c.transform.parent = transform;
                    LineRenderer lr = c.GetComponent<LineRenderer>();
                    initLineRenderer(lr);
                    yConnectors.Add(c);
                }
            }
        }
        //创建Z方向连接线
        zConnectors = new List<GameObject>();
        for (int i = 0; i < xControlPointNumber; ++i)
        {
            for (int j = 0; j < yControlPointNumber; ++j)
            {
                for (int k = 0; k < zControlPointNumber - 1; ++k)
                {
                    GameObject c = Instantiate(connector, transform.position, Quaternion.identity) as GameObject;
                    c.transform.parent = transform;
                    LineRenderer lr = c.GetComponent<LineRenderer>();
                    initLineRenderer(lr);
                    zConnectors.Add(c);
                }
            }
        }
    }

    //移动连接线
    private void moveConnectors()
    {
        //移动X方向连接线
        for (int i = 0; i < xControlPointNumber - 1; ++i)
        {
            for (int j = 0; j < yControlPointNumber; ++j)
            {
                for (int k = 0; k < zControlPointNumber; ++k)
                {                    
                    LineRenderer lr = xConnectors[i * (yControlPointNumber * zControlPointNumber) + j * zControlPointNumber + k].GetComponent<LineRenderer>();
                    setLineRendererPosition(lr, controlPoints[getControlPointIndex(i, j, k)].transform.position, controlPoints[getControlPointIndex(i + 1, j, k)].transform.position);
                }
            }
        }
        //移动Y方向连接线
        for (int i = 0; i < xControlPointNumber; ++i)
        {
            for (int j = 0; j < yControlPointNumber - 1; ++j)
            {
                for (int k = 0; k < zControlPointNumber; ++k)
                {
                    LineRenderer lr = yConnectors[i * ((yControlPointNumber - 1) * zControlPointNumber) + j * zControlPointNumber + k].GetComponent<LineRenderer>();
                    setLineRendererPosition(lr, controlPoints[getControlPointIndex(i, j, k)].transform.position, controlPoints[getControlPointIndex(i, j + 1, k)].transform.position);
                }
            }
        }
        //移动Z方向连接线
        for (int i = 0; i < xControlPointNumber; ++i)
        {
            for (int j = 0; j < yControlPointNumber; ++j)
            {
                for (int k = 0; k < zControlPointNumber - 1; ++k)
                {
                    LineRenderer lr = zConnectors[i * (yControlPointNumber * (zControlPointNumber - 1)) + j * (zControlPointNumber - 1) + k].GetComponent<LineRenderer>();
                    setLineRendererPosition(lr, controlPoints[getControlPointIndex(i, j, k)].transform.position, controlPoints[getControlPointIndex(i, j, k + 1)].transform.position);
                }
            }
        }
    }

    //FFD算法，计算顶点坐标
    private Vector3[] calculateVertices()
    {
        Vector3[] vertices = new Vector3[mesh.vertexCount];
        if (isCPUMode || computeShader == null)
        {
            for (int index = 0; index < mesh.vertexCount; ++index)
            {
                vertices[index] = Vector3.zero;
                for (int i = 0; i < xControlPointNumber; ++i)
                {
                    for (int j = 0; j < yControlPointNumber; ++j)
                    {
                        for (int k = 0; k < zControlPointNumber; ++k)
                        {
                            float f = xBernsteinPolynomials[index * xControlPointNumber + i] * yBernsteinPolynomials[index * yControlPointNumber + j] * zBernsteinPolynomials[index * zControlPointNumber + k];
                            vertices[index] += controlPoints[getControlPointIndex(i, j, k)].transform.localPosition * f;
                        }
                    }
                }
            } 
        }
        else
        {
            ComputeBuffer controlPointBuffer = new ComputeBuffer(controlPoints.Count, sizeof(float) * 3);
            ComputeBuffer xBernsteinPolynomialBuffer = new ComputeBuffer(xBernsteinPolynomials.Length, sizeof(float));
            ComputeBuffer yBernsteinPolynomialBuffer = new ComputeBuffer(yBernsteinPolynomials.Length, sizeof(float));
            ComputeBuffer zBernsteinPolynomialBuffer = new ComputeBuffer(zBernsteinPolynomials.Length, sizeof(float));
            ComputeBuffer vertexBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3);            
            List<Vector3> list = new List<Vector3>();
            foreach(GameObject o in controlPoints) list.Add(o.transform.localPosition);
            controlPointBuffer.SetData(list.ToArray());
            xBernsteinPolynomialBuffer.SetData(xBernsteinPolynomials);
            yBernsteinPolynomialBuffer.SetData(yBernsteinPolynomials);
            zBernsteinPolynomialBuffer.SetData(zBernsteinPolynomials);
            computeShader.SetInts(CONTROL_POINT_NUMBER_NAME, new int[] { xControlPointNumber, yControlPointNumber, zControlPointNumber });
            computeShader.SetBuffer(vertexKernelId, CONTROL_POINT_BUFFER_NAME, controlPointBuffer);            
            computeShader.SetBuffer(vertexKernelId, X_BERNSTEIN_POLYNOMIAL_BUFFER_NAME, xBernsteinPolynomialBuffer);
            computeShader.SetBuffer(vertexKernelId, Y_BERNSTEIN_POLYNOMIAL_BUFFER_NAME, yBernsteinPolynomialBuffer);
            computeShader.SetBuffer(vertexKernelId, Z_BERNSTEIN_POLYNOMIAL_BUFFER_NAME, zBernsteinPolynomialBuffer);
            computeShader.SetBuffer(vertexKernelId, OUTPUT_VERTEX_BUFFER_NAME, vertexBuffer);
            computeShader.Dispatch(vertexKernelId, Mathf.CeilToInt((float)mesh.vertexCount / THREAD_GROUPS_X), 1, 1);
            vertexBuffer.GetData(vertices);//取回结果
            controlPointBuffer.Release();
            xBernsteinPolynomialBuffer.Release();
            yBernsteinPolynomialBuffer.Release();
            zBernsteinPolynomialBuffer.Release();
            vertexBuffer.Release();
        }
        return vertices;
    }

    //FFD算法，计算顶点法线方向
    private Vector3[] calculateNormals(Vector3[] vertices)
    {
        Vector3[] normals = new Vector3[vertices.Length];
        if (isCPUMode || computeShader == null)
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                Vector3 normal = Vector3.zero;
                int begin = adjacentTriangleOffsets[i];
                int end = begin + adjacentTriangleSizes[i];
                for (int j = begin; j < end; ++j)
                {
                    Triangle t = adjacentTriangles[j];
                    Vector3 v0 = vertices[t.i1] - vertices[t.i0];
                    Vector3 v1 = vertices[t.i2] - vertices[t.i0];
                    normal += Vector3.Cross(v0, v1);
                }
                normal.Normalize();
                normals[i] = normal;
            }
        }
        else
        {
            ComputeBuffer vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
            ComputeBuffer triangleBuffer = new ComputeBuffer(adjacentTriangles.Length, sizeof(int) * 3);
            ComputeBuffer offsetBuffer = new ComputeBuffer(vertices.Length, sizeof(int));
            ComputeBuffer sizeBuffer = new ComputeBuffer(vertices.Length, sizeof(int));
            ComputeBuffer normalBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
            vertexBuffer.SetData(vertices);
            triangleBuffer.SetData(adjacentTriangles);
            offsetBuffer.SetData(adjacentTriangleOffsets);
            sizeBuffer.SetData(adjacentTriangleSizes);
            computeShader.SetBuffer(normalKernelId, VERTEX_BUFFER_NAME, vertexBuffer);
            computeShader.SetBuffer(normalKernelId, TRIANGLE_BUFFER_NAME, triangleBuffer);
            computeShader.SetBuffer(normalKernelId, OFFSET_BUFFER_NAME, offsetBuffer);
            computeShader.SetBuffer(normalKernelId, SIZE_BUFFER_NAME, sizeBuffer);
            computeShader.SetBuffer(normalKernelId, OUTPUT_NORMAL_BUFFER_NAME, normalBuffer);
            computeShader.Dispatch(normalKernelId, Mathf.CeilToInt((float)vertices.Length / THREAD_GROUPS_X), 1, 1);
            normalBuffer.GetData(normals);//取回结果
            vertexBuffer.Release();
            triangleBuffer.Release();
            offsetBuffer.Release();
            sizeBuffer.Release();
            normalBuffer.Release();
        }        
        return normals;
    }

    public GameObject getControlPoint(int x, int y, int z)
    {
        return controlPoints[getControlPointIndex(x, y, z)];
    }

    //将控制点的信息生成为字符串
    public string encodeToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#dimension#");
        sb.AppendLine("3");
        sb.AppendLine("#one to one#");
        sb.AppendLine("1");
        sb.AppendLine("#control grid size#");
        sb.AppendLine(xControlPointNumber.ToString());
        sb.AppendLine(yControlPointNumber.ToString());
        sb.AppendLine(zControlPointNumber.ToString());
        sb.AppendLine("#control grid spacing#");
        sb.AppendLine("1");
        sb.AppendLine("1");
        sb.AppendLine("1");
        sb.AppendLine("#offsets of the control points#");
        for (int i = 0; i < xControlPointNumber; ++i)
        {
            for (int j = 0; j < yControlPointNumber; ++j)
            {
                for (int k = 0; k < zControlPointNumber; ++k)
                {
                    Vector3 p = getControlPoint(i, j, k).transform.localPosition;
                    sb.AppendFormat("{0} {1} {2}", p.x, p.y, p.z);
                    sb.Append("\t");
                }
                sb.AppendLine();
            }
            sb.AppendLine();
        }
        sb.AppendLine("#quaternion qf,qb,qc,qd,qx,qy,qz#");
        Quaternion q = transform.localRotation;
        Vector3 v = transform.localPosition;
        sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", q.w, q.x, q.y, q.z, v.x, v.y, v.z);
        sb.AppendLine();
        return sb.ToString();
    }

    //从字符串读取信息
    public void decodeFromString(string s)
    {
        try
        {
            string numberRegex = @"#control grid size#\s+(\S+)\s+(\S+)\s+(\S+)";
            GroupCollection gc = Regex.Match(s, numberRegex, RegexOptions.Multiline).Groups;
            int xNumber = int.Parse(gc[1].Value);
            int yNumber = int.Parse(gc[2].Value);
            int zNumber = int.Parse(gc[3].Value);
            List<Vector3> list = new List<Vector3>();
            string positionRegex = @"([-\.0-9Ee]+) ([-\.0-9Ee]+) ([-\.0-9Ee]+)";
            MatchCollection mc = Regex.Matches(s, positionRegex, RegexOptions.Multiline);
            foreach (Match m in mc)
            {
                Vector3 v = new Vector3(float.Parse(m.Groups[1].Value), float.Parse(m.Groups[2].Value), float.Parse(m.Groups[3].Value));
                list.Add(v);
            }
            
            if (list.Count == xNumber * yNumber * zNumber)
            {
                initControlPoints(xNumber, yNumber, zNumber);
                for (int i = 0; i < list.Count; ++i)
                {
                    controlPoints[i].transform.localPosition = list[i];
                }
                if (reloadEvent != null)
                {
                    reloadEvent();
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public bool setComputeMode(bool isCPUMode)
    {
        if (computeShader == null)
        {
            this.isCPUMode = true;
            return isCPUMode;
        }
        else
        {
            this.isCPUMode = isCPUMode;
            return true;
        }
    }
}
