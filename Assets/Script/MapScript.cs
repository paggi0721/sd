using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Prop = TileUtil.Prop;

public class MapScript : MonoBehaviour
{
    [SerializeField]
    private Texture[] _texture;
    [SerializeField]
    private float _tileSize = 1F;
    [SerializeField]
    private int _mapSize = 11;


    private Mesh[] _mesh;
    private Material[] _mat;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            RandomGen();
        }

        for (int i = 0; i < _texture.Length; i++)
        {
            Graphics.DrawMesh(_mesh[i], Matrix4x4.identity, _mat[i], 0);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        RandomGen();
    }


    void RandomGen()
    {
        int nLayer = _texture.Length;

        _mesh = new Mesh[nLayer];
        _mat = new Material[nLayer];

        float fillRate = 1F;
        for(int i = 0; i < nLayer; ++i)
        {
            _mat[i] = TileUtil.MatTransparent(_texture[i], 3000 + i);

            bool[,] map = TestUtil.GenMap(_mapSize, _mapSize, fillRate);
            Prop[,] pmap = TileUtil.PropMap(map);

            _mesh[i] = TileUtil.BuildMesh(pmap, _tileSize);

            fillRate *= 0.8f;
        }
    }
}

static class TestUtil
{
    public static bool[,] GenMap(int row, int col, float fillRate)
    {
        bool[,] rt = new bool[row, col];

        if (fillRate >= 1F)
        {
            for (int i = 0; i < row; ++i)
                for (int j = 0; j < col; ++j)
                    rt[i, j] = true;
        }
        else
        {
            int fillCnt = (int)(row * col * fillRate);
            int r, c;
            for (int i = 0; i < fillCnt; ++i)
            {
                do
                {
                    r = Random.Range(0, row);
                    c = Random.Range(0, col);
                } while (rt[r, c]);

                rt[r, c] = true;
            }
        }

        return rt;
    }
}

static class TileUtil
{
    public struct Prop
    {
        public bool lt, rt;
        public bool lb, rb;

        public bool any { get { return lt || rt || lb || rb; } }
        public bool all { get { return lt && rt && lb && rb; } }
        public bool empty { get { return !any; } }

        public Prop(bool lt, bool rt, bool lb, bool rb)
        {
            this.lt = lt; this.rt = rt;
            this.lb = lb; this.rb = rb;
        }

        public override int GetHashCode()
        {
            int hash = 0;

            if (lt) hash |= (1 << 0);
            if (rt) hash |= (1 << 1);
            if (lb) hash |= (1 << 2);
            if (rb) hash |= (1 << 3);

            return hash;
        }
        public bool Equals(Prop rhs)
        {
            return lt == rhs.lt && rt == rhs.rt
                && lb == rhs.lb && rb == rhs.rb;
        }
    }

    struct PropCmp : IEqualityComparer<Prop>
    {
        public bool Equals(Prop lhs, Prop rhs)
        {
            return lhs.Equals(rhs);
        }
        public int GetHashCode(Prop p)
        {
            return p.GetHashCode();
        }
    }

    public static Prop[,] PropMap(bool[,] map)
    {
        int R = map.GetLength(0);
        int C = map.GetLength(1);

        Prop[,] pmap = new Prop[R + 1, C + 1];

        for (int i = 0; i < R; ++i)
        {
            for (int j = 0; j < C; ++j)
            {
                if (!map[i, j])
                    continue;

                pmap[i + 0, j + 0].rb = pmap[i + 0, j + 1].lb = true;
                pmap[i + 1, j + 0].rt = pmap[i + 1, j + 1].lt = true;
            }
        }

        return pmap;
    }

    static Dictionary<Prop, Vector2[]> lookupUvRect;
    static TileUtil()
    {
        lookupUvRect = new Dictionary<Prop, Vector2[]>(14, new PropCmp());

        const bool T = true;
        const bool F = false;   //LT RT LB RB
        lookupUvRect.Add(new Prop(F, F, F, T), uvs[0]);
        lookupUvRect.Add(new Prop(F, F, T, F), uvs[1]);
        lookupUvRect.Add(new Prop(F, F, T, T), uvs[2]);

        lookupUvRect.Add(new Prop(F, T, F, F), uvs[3]);
        lookupUvRect.Add(new Prop(F, T, F, T), uvs[4]);
        lookupUvRect.Add(new Prop(F, T, T, F), uvs[5]);
        lookupUvRect.Add(new Prop(F, T, T, T), uvs[6]);

        lookupUvRect.Add(new Prop(T, F, F, F), uvs[7]);
        lookupUvRect.Add(new Prop(T, F, F, T), uvs[8]);
        lookupUvRect.Add(new Prop(T, F, T, F), uvs[9]);
        lookupUvRect.Add(new Prop(T, F, T, T), uvs[10]);

        lookupUvRect.Add(new Prop(T, T, F, F), uvs[11]);
        lookupUvRect.Add(new Prop(T, T, F, T), uvs[12]);
        lookupUvRect.Add(new Prop(T, T, T, F), uvs[13]);
    }

    //  +v
    //
    //   |
    //   |
    // 0 *---* 1
    //   | / |
    // 2 *---* 3 --- +u
    public static Vector2[] RectUv(Prop p)
    {
        if (p.all)
        {
            int i = Random.Range(14, 32);
            return uvs[i];
        }
        else
        {
            return lookupUvRect[p];
        }
    }

    // 0 *---* 1 ---- +X
    //   | / |
    // 2 *---* 3
    //   |
    //   |
    //   
    //  -Z
    public static Vector3[] RectPt(int row, int col, float size)
    {
        Vector3[] rt =
        {
            Vector3.zero,
            Vector3.zero,
            Vector3.zero,
            Vector3.zero,
        };

        row = -row;

        rt[0].x = (col + 0) * size; rt[1].x = (col + 1) * size;
        rt[0].z = (row + 0) * size; rt[1].z = (row + 0) * size;

        rt[2].x = (col + 0) * size; rt[3].x = (col + 1) * size;
        rt[2].z = (row - 1) * size; rt[3].z = (row - 1) * size;

        return rt;
    }
    public static int[] RectTri(int begin)
    {
        return new int[] {
              begin + 0, begin + 1, begin + 2
            , begin + 1, begin + 3, begin + 2};
    }
    //       +v
    //        |
    //        |
    //   1.00 *-----*-----*-----*-----*-----*-----*-----*-----*(1.0, 1.0)
    //        |     |     |     |     |     |     |     |     |       
    //        |15   |0    |1    |2    |16   |17   |18   |19   |       
    //   0.75 *-----*-----*-----*-----*-----*-----*-----*-----*      0.75
    //        |     |     |     |     |     |     |     |     |          
    //        |3    |4    |5    |6    |20   |21   |22   |23   |          
    //   0.50 *-----*-----*-----*-----*-----*-----*-----*-----*      0.50
    //        |     |     |     |     |     |     |     |     |          
    //        |7    |8    |9    |10   |24   |25   |26   |27   |          
    //   0.25 *-----*-----*-----*-----*-----*-----*-----*-----*      0.25
    //        |     |     |     |     |     |     |     |     |          
    //        |11   |12   |13   |14   |28   |29   |30   |31   |          
    //  (0, 0)*-----*-----*-----*-----*-----*-----*-----*-----*      0.00 --- +u
    //       0.0   0.125 0.25  0.375 0.5   0.625 0.75  0.876 1.0

    //  +v
    //
    //   |
    //   |
    // 0 *---* 1
    //   | / |
    // 2 *---* 3 --- +u
    static Vector2 UV(int r, int c)
    {
        const float W = 0.125F;
        const float H = 0.25F;
        return new Vector2(c * W, r * H);
    }
    static Vector2[][] uvs = new Vector2[32][]
    {
        new Vector2[]{UV(4,1), UV(4,2), UV(3,1), UV(3,2)},  // 0
        new Vector2[]{UV(4,2), UV(4,3), UV(3,2), UV(3,3)},  // 1
        new Vector2[]{UV(4,3), UV(4,4), UV(3,3), UV(3,4)},  // 2

        new Vector2[]{UV(3,0), UV(3,1), UV(2,0), UV(2,1)},  // 3
        new Vector2[]{UV(3,1), UV(3,2), UV(2,1), UV(2,2)},  // 4
        new Vector2[]{UV(3,2), UV(3,3), UV(2,2), UV(2,3)},  // 5
        new Vector2[]{UV(3,3), UV(3,4), UV(2,3), UV(2,4)},  // 6

        new Vector2[]{UV(2,0), UV(2,1), UV(1,0), UV(1,1)},  // 7
        new Vector2[]{UV(2,1), UV(2,2), UV(1,1), UV(1,2)},  // 8
        new Vector2[]{UV(2,2), UV(2,3), UV(1,2), UV(1,3)},  // 9
        new Vector2[]{UV(2,3), UV(2,4), UV(1,3), UV(1,4)},  //10

        new Vector2[]{UV(1,0), UV(1,1), UV(0,0), UV(0,1)},  //11
        new Vector2[]{UV(1,1), UV(1,2), UV(0,1), UV(0,2)},  //12
        new Vector2[]{UV(1,2), UV(1,3), UV(0,2), UV(0,3)},  //13

        new Vector2[]{UV(1,3), UV(1,4), UV(0,3), UV(0,4)},  //14
        new Vector2[]{UV(4,0), UV(4,1), UV(3,0), UV(3,1)},  //15
        
        new Vector2[]{UV(4,4), UV(4,5), UV(3,4), UV(3,5)},  //16
        new Vector2[]{UV(4,5), UV(4,6), UV(3,5), UV(3,6)},  //17
        new Vector2[]{UV(4,6), UV(4,7), UV(3,6), UV(3,7)},  //18
        new Vector2[]{UV(4,7), UV(4,8), UV(3,7), UV(3,8)},  //19
        
        new Vector2[]{UV(3,4), UV(3,5), UV(2,4), UV(2,5)},  //20
        new Vector2[]{UV(3,5), UV(3,6), UV(2,5), UV(2,6)},  //21
        new Vector2[]{UV(3,6), UV(3,7), UV(2,6), UV(2,7)},  //22
        new Vector2[]{UV(3,7), UV(3,8), UV(2,7), UV(2,8)},  //23
        
        new Vector2[]{UV(2,4), UV(2,5), UV(1,4), UV(1,5)},  //24
        new Vector2[]{UV(2,5), UV(2,6), UV(1,5), UV(1,6)},  //25
        new Vector2[]{UV(2,6), UV(2,7), UV(1,6), UV(1,7)},  //26
        new Vector2[]{UV(2,7), UV(2,8), UV(1,7), UV(1,8)},  //27
        
        new Vector2[]{UV(1,4), UV(1,5), UV(0,4), UV(0,5)},  //28
        new Vector2[]{UV(1,5), UV(1,6), UV(0,5), UV(0,6)},  //29
        new Vector2[]{UV(1,6), UV(1,7), UV(0,6), UV(0,7)},  //30
        new Vector2[]{UV(1,7), UV(1,8), UV(0,7), UV(0,8)},  //31
    };

    public static Vector2[] RectUv(int index)
    {
        return uvs[index];
    }

    static List<Vector3> listVtx = new List<Vector3>();
    static List<Vector2> listUvs = new List<Vector2>();
    static List<int> listTri = new List<int>();

    public static Mesh BuildMesh(Prop[,] pmap, float size)
    {
        int R = pmap.GetLength(0);
        int C = pmap.GetLength(1);

        //listVtx.Capacity = Mathf.Max(R * C * 4, listVtx.Capacity);
        //listUvs.Capacity = Mathf.Max(R * C * 4, listUvs.Capacity);
        //listTri.Capacity = Mathf.Max(R * C * 6, listTri.Capacity);

        listVtx.Clear(); listVtx.Capacity = Mathf.Max(R * C * 4, listVtx.Capacity);
        listUvs.Clear(); listUvs.Capacity = Mathf.Max(R * C * 4, listUvs.Capacity);
        listTri.Clear(); listTri.Capacity = Mathf.Max(R * C * 6, listTri.Capacity);

        for (int i = 0; i < R; ++i)
        {
            for (int j = 0; j < C; ++j)
            {
                Prop p = pmap[i, j];
                if (p.empty)
                    continue;

                int bi = listVtx.Count;
                listVtx.AddRange(RectPt(i, j, size));
                listUvs.AddRange(RectUv(p));
                listTri.AddRange(RectTri(bi));
            }
        }

        Mesh rt = new Mesh();

        rt.vertices = listVtx.ToArray();
        rt.uv = listUvs.ToArray();
        rt.triangles = listTri.ToArray();

        rt.RecalculateNormals();
        rt.RecalculateTangents();
        rt.RecalculateBounds();

        return rt;
    }

    public static Material MatTransparent(Texture texture, int rq)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetTexture("_MainTex", texture);

        mat.SetFloat("_Mode", 3.0f);
        mat.SetOverrideTag("RenderType", "Opaque");
        //mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        //mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);

        mat.EnableKeyword("_ALPHATEST_ON");
        //mat.DisableKeyword("_ALPHABLEND_ON");
        //mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");

        mat.renderQueue = rq;

        return mat;
    }
}
