﻿using GeoJSON.Net.Feature;
using System.Collections.Generic;
using UnityEngine;

namespace Virgis
{

	/// <summary>
	/// Class for holding PointCloud data as a Particle cloud
	/// 
	/// </summary>
	public class ParticleData
	{
		public List<Vector3> vertices;
		public List<Vector3> normals;
		public List<Color32> colors;
		public int vertexCount;
		public Bounds bounds;

        public ParticleData() {
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            colors = new List<Color32>();
            vertexCount = 0;
            bounds = new Bounds();
        }
	}
}

