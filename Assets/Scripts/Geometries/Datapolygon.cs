// copyright Runette Software Ltd, 2020. All rights reserved
// parts from https://answers.unity.com/questions/546473/create-a-plane-from-points.html

using System.Collections.Generic;
using UnityEngine;
using g3;

namespace Virgis
{
 

    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component
    /// </summary>
    public class Datapolygon : VirgisFeature
    {

        private bool BlockMove = false; // Is this component in a block move state
        private GameObject Shape; // gameObject to be used for the shape
        public Vector3 Centroid; // Polyhedral center vertex
        public List<VertexLookup> VertexTable;


        public override void Selected(SelectionTypes button)
        {
            if (button == SelectionTypes.SELECTALL)
            {
                gameObject.BroadcastMessage("Selected", SelectionTypes.BROADCAST, SendMessageOptions.DontRequireReceiver);
                BlockMove = true;
                Dataline com = gameObject.GetComponentInChildren<Dataline>();
                com.Selected(SelectionTypes.SELECTALL);
            }
        }

        public override void UnSelected(SelectionTypes button)
        {
            if (button != SelectionTypes.BROADCAST)
            {
                gameObject.BroadcastMessage("UnSelected", SelectionTypes.BROADCAST, SendMessageOptions.DontRequireReceiver);
                BlockMove = false;
            }
        }

        public override void VertexMove(MoveArgs data)
        {
            if (!BlockMove)
            {
                ShapeMoveVertex(data);
            }
        }

        public override void Translate(MoveArgs args)
        {
            if (BlockMove)
            {
                GameObject shape = gameObject.transform.Find("Polygon Shape").gameObject;
                shape.transform.Translate(args.translate, Space.World);
            }

        }

        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public override void MoveAxis(MoveArgs args)
        {
            if (args.translate != null)
            {
                Shape.transform.Translate(args.translate, Space.World);
            }
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            Shape.transform.RotateAround(args.pos, axis, angle);
            Vector3 A = Shape.transform.localPosition;
            Vector3 B = transform.InverseTransformPoint(args.pos);
            Vector3 C = A - B;
            float RS = args.scale;
            Vector3 FP = B + C * RS;
            if (FP.magnitude < float.MaxValue)
            {
                Shape.transform.localScale = Shape.transform.localScale * RS;
                Shape.transform.localPosition = FP;
            }
        }

        public override void MoveTo(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }



        /// <summary>
        /// Called to draw the Polygon based upon the 
        /// </summary>
        /// <param name="perimeter">LineString defining the perimter of the polygon</param>
        /// <param name="mat"> Material to be used</param>
        /// <returns></returns>
        public GameObject Draw( List<VertexLookup> verteces,  Material mat = null)
        {
            
            VertexTable = verteces;
            
            Shape = new GameObject("Polygon Shape");
            Shape.transform.parent = gameObject.transform;
            Shape.transform.position = Centroid;

            MakeMesh();

            Renderer rend = Shape.GetComponent<MeshRenderer>();
            if (rend == null)
            {
                rend = Shape.AddComponent<MeshRenderer>();
                rend.material = mat;
            };

            return gameObject;

        }

        /// <summary>
        /// Generates the actual mesh for the polyhedron
        /// </summary>
        private void MakeMesh()
        {
            MeshFilter mf;
            mf = Shape.GetComponent<MeshFilter>();    
            if (mf == null)  mf = Shape.AddComponent<MeshFilter>();
            mf.mesh = null;
            Mesh mesh = new Mesh();
            Vector3[] vertices = Vertices();
            mesh.vertices = vertices;
            mesh.triangles = Triangles(vertices.Length - 1);
            mesh.uv = BuildUVs(vertices);

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            mf.mesh = mesh;
        }


        /// <summary>
        /// Move a vertex of the polygon and recreate the mesh
        /// </summary>
        /// <param name="data">MoveArgs</param>
        public void ShapeMoveVertex(MoveArgs data)
        {
            Mesh mesh = Shape.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            vertices[VertexTable.Find(item => item.Id == data.id ).Vertex + 1] = Shape.transform.InverseTransformPoint(data.pos);
            mesh.vertices = vertices;
            mesh.uv = BuildUVs(vertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        public override VirgisFeature AddVertex(Vector3 position) {
            _redraw();
            return base.AddVertex(position);
        }

        public override void RemoveVertex(VirgisFeature vertex) {
            if (BlockMove) {
                gameObject.Destroy();
            } else {
                _redraw();
            }
        }

        private void _redraw() {
            VertexTable = GetComponentInChildren<Dataline>().VertexTable;
            MakeMesh();
        }


        /// <summary>
        /// Calculate the verteces of the polygon from the LineSString
        /// </summary>
        /// <param name="poly">Vector3[] LineString in Worlspace coordinates</param>
        /// <param name="center">Vector3 centroid in Worldspace coordinates</param>
        /// <returns></returns>
        public Vector3[] Vertices()
        {
            Vector3[] vertices = new Vector3[VertexTable.Count];
            vertices[0] = Shape.transform.InverseTransformPoint(Centroid);


            for (int i = 0; i < VertexTable.Count - 1; i++)
            {
                vertices[i + 1] = Shape.transform.InverseTransformPoint(VertexTable.Find(item => item.Vertex == i).Com.transform.position);
            }

            return vertices;
        }

        // STATIC METHODS TO HELP CREATE A POLYGON

        /// <summary>
        /// calculate the Triangles for a Polyhrderon with length verteces
        /// </summary>
        /// <param name="length">number of verteces not including the centroid</param>
        /// <returns></returns>
        public static int[] Triangles(int length)
        {
            
            int[] triangles = new int[length * 3];

            for (int i = 0; i < length - 1; i++)
            {
                triangles[i * 3] = i + 2;
                triangles[i * 3 + 1] = 0;
                triangles[i * 3 + 2] = i + 1;
            }

            triangles[(length - 1) * 3] = 1;
            triangles[(length - 1) * 3 + 1] = 0;
            triangles[(length - 1) * 3 + 2] = length;

            return triangles;
        }


        /// <summary>
        /// Reset the center vertex to be the center of the Linear Ring vertexes
        /// </summary>
        public void ResetCenter() {
            VertexLookup centroid = VertexTable.Find(item => item.Vertex == -1);
            DCurve3 curve = new DCurve3();
            curve.Vector3(GetVertexPositions(), true);
            centroid.Com.transform.position = (Vector3)curve.Center();
            MakeMesh();
        }

        static Vector2[] BuildUVs(Vector3[] vertices)
        {

            float xMin = Mathf.Infinity;
            float yMin = Mathf.Infinity;
            float xMax = -Mathf.Infinity;
            float yMax = -Mathf.Infinity;

            Vector3[] UVWs = new Vector3[vertices.Length];

            Vector3[] edges = new Vector3[vertices.Length];

            edges[0] = Vector3.zero;

            for (int i = 1; i< vertices.Length; i++)
            {
                edges[i] = vertices[0] - vertices[i];
            }

            UVWs[1] = Vector3.zero;
            Vector3 baselineEdge = vertices[vertices.Length - 1] - vertices[1];
            UVWs[vertices.Length - 1] = Vector3.right * baselineEdge.magnitude;
            float theta = Vector3.Angle(baselineEdge, edges[vertices.Length - 1]);
            UVWs[0] = UVWs[vertices.Length - 1] + Quaternion.Euler(0, 0, theta) * Vector3.right * edges[vertices.Length - 1].magnitude;

            float thetaStash = 0;
  
            for (int i = 2; i < vertices.Length -1 ; i++)
            {
                theta = Vector3.Angle(edges[1], edges[i]);
                if (theta < thetaStash) theta = 360 - theta;
                thetaStash = theta;
                UVWs[i] = UVWs[0] + Quaternion.Euler(0, 0, 180 - theta) * UVWs[0].normalized * edges[i].magnitude;
            }

            foreach (Vector3 v3 in UVWs)
            {
                if (v3.x < xMin)
                    xMin = v3.x;
                if (v3.y < yMin)
                    yMin = v3.y;
                if (v3.x > xMax)
                    xMax = v3.x;
                if (v3.y > yMax)
                    yMax = v3.y;
            }

            float xRange = xMax - xMin;
            float yRange = yMax - yMin;

            Vector2[] uvs = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i].x = (UVWs[i].x - xMin) / xRange;
                uvs[i].y = (UVWs[i].y - yMin) / yRange;


            }
            return uvs;
        }

        /// <summary>
        /// Get an array of the Datapoint components for the vertexes
        /// </summary>
        /// <returns> Datapoint[]</returns>
        public Datapoint[] GetVertexes() {
            Datapoint[] result = new Datapoint[VertexTable.Count - 1];
            for (int i = 0; i < result.Length; i++) {
                result[i] = VertexTable.Find(item => item.isVertex && item.Vertex == i).Com as Datapoint;
            }
            return result;
        }

    
        public Vector3[] GetVertexPositions() {
            return GetComponentInChildren<Dataline>().GetVertexPositions();
        }
    }
}

