﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Project;
using g3;

public class MeshLayer : MonoBehaviour, ILayer
{

    public string filename;
    public Material material;
    public GameObject handle;
    public List<GameObject> meshes;
    public bool changed { get; set; }
    public RecordSet layer { get; set; }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    private async Task<SimpleMeshBuilder> loadObj(string filename)
    {
        using (StreamReader reader = File.OpenText(filename))
        {
            OBJReader objReader = new OBJReader();
            SimpleMeshBuilder meshBuilder = new SimpleMeshBuilder();
            IOReadResult result = objReader.Read(reader, new ReadOptions(), meshBuilder);
            return meshBuilder;
        }
    }

    public async Task<GameObject> Init(GeographyCollection layer)
    {
        this.layer = layer;
        string source = layer.Source;
        Quaternion rotate = layer.Transform.Rotate;
        Vector3 scale = layer.Transform.Scale;
        Vector3 translate = (Vector3)layer.Transform.Position * Global._map.WorldRelativeScale;
        Dictionary<string, Unit> symbology = layer.Properties.Units;
        meshes = new List<GameObject>();

        if (source != null)
        {
            SimpleMeshBuilder meshBuilder = await loadObj(source);
            foreach (SimpleMesh simpleMesh in meshBuilder.Meshes)
            {
                GameObject meshGameObject = new GameObject();
                MeshFilter mf = meshGameObject.AddComponent<MeshFilter>();
                MeshRenderer renderer = meshGameObject.AddComponent<MeshRenderer>();
                renderer.material = material;
                meshGameObject.transform.parent = gameObject.transform;
                meshGameObject.transform.localPosition = Vector3.zero;
                meshGameObject.transform.Translate(translate);
                meshGameObject.transform.localRotation = rotate;
                meshGameObject.transform.localScale = scale;
                mf.mesh = simpleMesh.ToMesh();
                meshes.Add(meshGameObject);
            }
            GameObject centreHandle = Instantiate(handle, gameObject.transform);
            centreHandle.transform.Translate(translate);
            centreHandle.SendMessage("SetColor", (Color)symbology["handle"].Color);
        }
        changed = false;
        return gameObject;
    }

    public void Translate(MoveArgs args)
    {
        foreach (GameObject mesh in meshes) {
            if (args.translate != Vector3.zero) mesh.transform.Translate(args.translate, Space.World);
            if (args.rotate != Quaternion.identity) mesh.transform.rotation = mesh.transform.rotation * args.rotate;
            if (args.scale != 0.0f) mesh.transform.localScale = mesh.transform.localScale * args.scale;
            changed = true;
        }
    }

    public void Save()
    {
        layer.Transform.Position = meshes[0].transform.localPosition / Global._map.WorldRelativeScale;
    }
}

