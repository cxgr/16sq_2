using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System.Text;

public class ModelLoader : MonoBehaviour
{
    [SerializeField]
    string defaultPath = "D:/testmodels/";

    [SerializeField]
    InputField pathInput;

    [SerializeField]
    Transform spawnPointsRoot;
    Transform[] spawnPoints;

    [SerializeField]
    Transform buttonsRoot;
    Button[] buttons;

    void Awake()
    {
        spawnPoints = spawnPointsRoot.GetComponentsInChildren<Transform>()
            .Where((t) => t.GetInstanceID() != spawnPointsRoot.GetInstanceID())
            .OrderBy((t) => t.name).ToArray();

        buttons = buttonsRoot.GetComponentsInChildren<Button>()
            .OrderBy((b) => b.name).ToArray();

        foreach (var b in buttons)
        {
            b.onClick.AddListener(() =>
            {
                var loadedFine = LoadModel(int.Parse(b.name));
                b.interactable = !loadedFine;
            });
        }

        pathInput.text = defaultPath;
    }

    public bool LoadModel(int idx)
    {
        var path = pathInput.text;
        var pathObj = path + idx.ToString() + ".obj";

        if (!File.Exists(pathObj))
        {
            Debug.LogError("no file found at " + pathObj);
            return false;
        }

        var buildingGO = new GameObject(idx.ToString());
        var mtlPath = new StringBuilder(path);
        var texPath = new StringBuilder(path);
        buildingGO.AddComponent<MeshFilter>().mesh = ParseObj(pathObj, mtlPath);
        buildingGO.AddComponent<MeshRenderer>().material = CreateMaterial(mtlPath, texPath);
        buildingGO.transform.localScale = new Vector3(-1f, 1f, 1f);
        buildingGO.transform.position = spawnPoints[idx - 1].position;
        return true;
    }

    struct TriData
    {
        public TriData(int v, int uv, int n) { this.vertIdx = v; this.uvIdx = uv; this.normIdx = n; }
        public int vertIdx;
        public int normIdx;
        public int uvIdx;
    }

    Mesh ParseObj(string path, StringBuilder mtlPath)
    {
        Debug.Log("parsing " + path);
        List<Vector3> vertsRaw = new List<Vector3>();
        List<Vector3> normsRaw = new List<Vector3>();
        List<Vector2> uvsRaw = new List<Vector2>();
        List<TriData> trisIdx = new List<TriData>();

        var allLines = File.ReadAllLines(path);
        for (int i = 0; i < allLines.Length; ++i)
        {
            var l = allLines[i];
            if (l.Length == 0 || l[0] == '#')
                continue;

            var lineParts = l.Trim().Split(' ');
            var idStr = lineParts[0];

            switch (idStr)
            {
                case "v":
                    vertsRaw.Add(LineToVector3(lineParts));
                    break;

                case "vn":
                    normsRaw.Add(LineToVector3(lineParts));
                    break;

                case "vt":
                    uvsRaw.Add(LineToVector2(lineParts));
                    break;

                case "f":
                    if (lineParts.Length != 4)
                    {
                        Debug.LogError("bad face indexing, probably not triangulated");
                        return null;
                    }
                    for (int j = 1; j <= 3; ++j)
                    {
                        var trisParts = lineParts[j].Split('/');

                        trisIdx.Add(new TriData(
                            int.Parse(trisParts[0]) - 1,
                            int.Parse(trisParts[1]) - 1,
                            int.Parse(trisParts[2]) - 1));
                    }
                    break;

                case "mtllib":
                    mtlPath.Append(lineParts[1]);
                    break;
            }
        }

        if (vertsRaw.Count > 64999 || trisIdx.Count > 64999)
        {
            Debug.LogError("65k limit exceeded");
            return null;
        }

        Debug.Log("OBJ STATS");
        Debug.Log("verts " + vertsRaw.Count);
        Debug.Log("normals " + normsRaw.Count);
        Debug.Log("uvs " + uvsRaw.Count);
        Debug.Log("tris " + trisIdx.Count);

        Vector3[] vertsFinal = new Vector3[trisIdx.Count];
        Vector2[] uvsFinal = new Vector2[trisIdx.Count];
        Vector3[] normsFinal = new Vector3[trisIdx.Count];
        int[] trisIndexer = new int[trisIdx.Count];

        for (int i = 0; i < trisIdx.Count; ++i)
        {
            var triData = trisIdx[i];
            vertsFinal[i] = vertsRaw[triData.vertIdx];
            uvsFinal[i] = uvsRaw[triData.uvIdx];
            normsFinal[i] = normsRaw[triData.normIdx];
            trisIndexer[i] = i;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertsFinal;
        mesh.uv = uvsFinal;
        mesh.normals = normsFinal;
        mesh.triangles = trisIndexer;
        mesh.RecalculateBounds();

        return mesh;
    }

    Material CreateMaterial(StringBuilder mtlPath, StringBuilder texPath)
    {
        Debug.Log("creating material from " + mtlPath);
        var mat = new Material(Shader.Find("Mobile/Diffuse"));
        var tex = new Texture2D(2, 2);
        mat.SetTexture("_MainTex", tex);

        var allLines = File.ReadAllLines(mtlPath.ToString());
        foreach (var l in allLines)
        {
            var parts = l.Trim().Split(' ');
            if (parts[0] == "map_Kd")
            {
                StartCoroutine(LoadTexture(tex, texPath.Append(parts[1])));
                break;
            }
        }

        return mat;
    }

    IEnumerator LoadTexture(Texture2D tex, StringBuilder path)
    {
        WWW www = new WWW(path.Insert(0, @"file://").ToString());
        yield return www;
        www.LoadImageIntoTexture(tex);
    }

    Vector2 LineToVector2(string[] line)
    {
        return new Vector2(float.Parse(line[1]), float.Parse(line[2]));
    }

    Vector3 LineToVector3(string[] line)
    {
        return new Vector3(float.Parse(line[1]), float.Parse(line[2]), float.Parse(line[3]));
    }
}