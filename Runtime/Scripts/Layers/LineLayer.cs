/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using OSGeo.OGR;
using Project;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Virgis
{

    /// <summary>
    /// The parent entity for a instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public class LineLayer : VirgisLayer<RecordSet, Layer>
    {
        // The prefab for the data points to be instantiated
        public GameObject CylinderLinePrefab; // Prefab to be used for cylindrical lines
        public GameObject CuboidLinePrefab; // prefab to be used for cuboid lines
        public GameObject SpherePrefab; // prefab to be used for Vertex handles
        public GameObject CubePrefab; // prefab to be used for Vertex handles
        public GameObject CylinderPrefab; // prefab to be used for Vertex handle
        public GameObject LabelPrefab;
        public Material PointBaseMaterial;
        public Material LineBaseMaterial;


        private GameObject m_handlePrefab;
        private GameObject m_linePrefab;
        private Dictionary<string, Unit> m_symbology;
        private Material m_mainMat;
        private Material m_selectedMat;
        private Material m_lineMain;
        private Material m_lineSelected;

        new protected void Awake() {
            base.Awake();
            featureType = FeatureType.LINE;
        }

        protected override async Task _init() {
            await Load();
        }

        protected Task<int> Load() {
            Task<int> t1 = new Task<int>(() => {
                RecordSet layer = _layer as RecordSet;
                m_symbology = layer.Properties.Units;

                if (m_symbology.ContainsKey("point") && m_symbology["point"].ContainsKey("Shape")) {
                    Shapes shape = m_symbology["point"].Shape;
                    switch (shape) {
                        case Shapes.Spheroid:
                            m_handlePrefab = SpherePrefab;
                            break;
                        case Shapes.Cuboid:
                            m_handlePrefab = CubePrefab;
                            break;
                        case Shapes.Cylinder:
                            m_handlePrefab = CylinderPrefab;
                            break;
                        default:
                            m_handlePrefab = SpherePrefab;
                            break;
                    }
                } else {
                    m_handlePrefab = SpherePrefab;
                }

                if (m_symbology.ContainsKey("line") && m_symbology["line"].ContainsKey("Shape")) {
                    Shapes shape = m_symbology["line"].Shape;
                    switch (shape) {
                        case Shapes.Cuboid:
                            m_linePrefab = CuboidLinePrefab;
                            break;
                        case Shapes.Cylinder:
                            m_linePrefab = CylinderLinePrefab;
                            break;
                        default:
                            m_linePrefab = CylinderLinePrefab;
                            break;
                    }
                } else {
                    m_linePrefab = CylinderLinePrefab;
                }

                Color col = m_symbology.ContainsKey("point") ? (Color) m_symbology["point"].Color : Color.white;
                Color sel = m_symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
                Color line = m_symbology.ContainsKey("line") ? (Color) m_symbology["line"].Color : Color.white;
                Color lineSel = m_symbology.ContainsKey("line") ? new Color(1 - line.r, 1 - line.g, 1 - line.b, line.a) : Color.red;
                m_mainMat = Instantiate(PointBaseMaterial);
                m_mainMat.SetColor("_BaseColor", col);
                m_selectedMat = Instantiate(PointBaseMaterial);
                m_selectedMat.SetColor("_BaseColor", sel);
                m_lineMain = Instantiate(LineBaseMaterial);
                m_lineMain.SetColor("_BaseColor", line);
                m_lineSelected = Instantiate(LineBaseMaterial);
                m_lineSelected.SetColor("_BaseColor", lineSel);
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        protected override VirgisFeature _addFeature(Vector3[] line)
        {
            Geometry geom = new Geometry(wkbGeometryType.wkbLineString25D);
            geom.AssignSpatialReference(AppState.instance.mapProj);
            geom.Vector3(line);
            return _drawFeature(geom, new Feature(new FeatureDefn(null)));
        }

        protected override async Task _draw()
        {
            RecordSet layer = GetMetadata();
            if (layer.Properties.BBox != null) {
                features.SetSpatialFilterRect(layer.Properties.BBox[0], layer.Properties.BBox[1], layer.Properties.BBox[2], layer.Properties.BBox[3]);
            }
            using (OgrReader ogrReader = new OgrReader()) {
                await ogrReader.GetFeaturesAsync(features);
                foreach (Feature feature in ogrReader.features) {
                    if (feature == null)
                        continue;
                    int geoCount = feature.GetDefnRef().GetGeomFieldCount();
                    for (int j = 0; j < geoCount; j++) {
                        Geometry line = feature.GetGeomFieldRef(j);
                        if (line == null)
                            continue;
                        if (line.GetGeometryType() == wkbGeometryType.wkbLineString ||
                            line.GetGeometryType() == wkbGeometryType.wkbLineString25D ||
                            line.GetGeometryType() == wkbGeometryType.wkbLineStringM ||
                            line.GetGeometryType() == wkbGeometryType.wkbLineStringZM
                        ) {
                            if (line.GetSpatialReference() == null)
                                line.AssignSpatialReference(GetCrs());
                            await _drawFeatureAsync(line, feature);
                        } else if
                            (line.GetGeometryType() == wkbGeometryType.wkbMultiLineString ||
                            line.GetGeometryType() == wkbGeometryType.wkbMultiLineString25D ||
                            line.GetGeometryType() == wkbGeometryType.wkbMultiLineStringM ||
                            line.GetGeometryType() == wkbGeometryType.wkbMultiLineStringZM
                         ) {
                            int n = line.GetGeometryCount();
                            for (int k = 0; k < n; k++) {
                                Geometry Line2 = line.GetGeometryRef(k);
                                if (Line2.GetSpatialReference() == null)
                                    Line2.AssignSpatialReference(GetCrs());
                                await _drawFeatureAsync(Line2, feature);
                            }
                        }
                        line.Dispose();
                    }
                }
            }
            if (layer.Transform != null) {
                transform.position = AppState.instance.map.transform.TransformPoint(layer.Transform.Position);
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
            }
        }


        /// <summary>
        /// Draws a single feature based on world scale coordinates
        /// </summary>
        /// <param name="line"> Vector3[] coordinates</param>
        /// <param name="feature">Featire (optinal)</param>
        protected VirgisFeature _drawFeature(Geometry line, Feature feature = null)
        {
            GameObject dataLine = Instantiate(m_linePrefab, transform, false);

            //set the gisProject properties
            Dataline com = dataLine.GetComponent<Dataline>();
            if (feature != null)
                com.feature = feature;

            //Draw the line
            com.Draw(line, m_symbology, m_handlePrefab, LabelPrefab, m_mainMat, m_selectedMat, m_lineMain, m_lineSelected);

            return com;
        }

        protected Task<int> _drawFeatureAsync(Geometry line, Feature feature = null) {

            Task<int> t1 = new Task<int>(() => {
                _drawFeature(line, feature);
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        protected override void _checkpoint()
        {
        }

        protected override Task _save()
        {
            Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
            foreach (Dataline dataFeature in dataFeatures) {
                Feature feature = dataFeature.feature;
                Geometry geom = new Geometry(wkbGeometryType.wkbLineString25D);
                geom.AssignSpatialReference(AppState.instance.mapProj);
                geom.Vector3(dataFeature.GetVertexPositions());
                geom.TransformTo(GetCrs());
                feature.SetGeometryDirectly(geom);
                features.SetFeature(feature);
            };
            features.SyncToDisk();
            return Task.CompletedTask;

        }

        public override GameObject GetFeatureShape()
        {
            GameObject fs = Instantiate(m_handlePrefab);
            Datapoint com = fs.GetComponent<Datapoint>();
            com.SetMaterial(m_mainMat, m_selectedMat);
            return fs;
        }

        public override void Translate(MoveArgs args)
        {
            changed = true;
            Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
            dataFeatures.ToList<Dataline>().Find(item => args.id == item.GetId())?.transform.Translate(args.translate, Space.World);
        }


        public override void MoveAxis(MoveArgs args)
        {
            changed = true;
            Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
            dataFeatures.ToList<Dataline>().Find(item => args.id == item.GetId()).MoveAxisAction(args);
        }

    }
}
