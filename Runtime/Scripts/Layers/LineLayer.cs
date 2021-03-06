// copyright Runette Software Ltd, 2020. All rights reserved

using OSGeo.OGR;
using Project;
using System.Collections.Generic;
using System.Collections;
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


        private GameObject HandlePrefab;
        private GameObject LinePrefab;
        private Dictionary<string, Unit> symbology;
        private Material mainMat;
        private Material selectedMat;
        private Material lineMain;
        private Material lineSelected;

        private void Start() {
            featureType = FeatureType.LINE;
        }

        protected override async Task _init() {
            await Load();
        }

        protected Task<int> Load() {
            Task<int> t1 = new Task<int>(() => {
                RecordSet layer = _layer as RecordSet;
                symbology = layer.Properties.Units;

                if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Shape")) {
                    Shapes shape = symbology["point"].Shape;
                    switch (shape) {
                        case Shapes.Spheroid:
                            HandlePrefab = SpherePrefab;
                            break;
                        case Shapes.Cuboid:
                            HandlePrefab = CubePrefab;
                            break;
                        case Shapes.Cylinder:
                            HandlePrefab = CylinderPrefab;
                            break;
                        default:
                            HandlePrefab = SpherePrefab;
                            break;
                    }
                } else {
                    HandlePrefab = SpherePrefab;
                }

                if (symbology.ContainsKey("line") && symbology["line"].ContainsKey("Shape")) {
                    Shapes shape = symbology["line"].Shape;
                    switch (shape) {
                        case Shapes.Cuboid:
                            LinePrefab = CuboidLinePrefab;
                            break;
                        case Shapes.Cylinder:
                            LinePrefab = CylinderLinePrefab;
                            break;
                        default:
                            LinePrefab = CylinderLinePrefab;
                            break;
                    }
                } else {
                    LinePrefab = CylinderLinePrefab;
                }

                Color col = symbology.ContainsKey("point") ? (Color) symbology["point"].Color : Color.white;
                Color sel = symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
                Color line = symbology.ContainsKey("line") ? (Color) symbology["line"].Color : Color.white;
                Color lineSel = symbology.ContainsKey("line") ? new Color(1 - line.r, 1 - line.g, 1 - line.b, line.a) : Color.red;
                mainMat = Instantiate(PointBaseMaterial);
                mainMat.SetColor("_BaseColor", col);
                selectedMat = Instantiate(PointBaseMaterial);
                selectedMat.SetColor("_BaseColor", sel);
                lineMain = Instantiate(LineBaseMaterial);
                lineMain.SetColor("_BaseColor", line);
                lineSelected = Instantiate(LineBaseMaterial);
                lineSelected.SetColor("_BaseColor", lineSel);
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
            GameObject dataLine = Instantiate(LinePrefab, transform, false);

            //set the gisProject properties
            Dataline com = dataLine.GetComponent<Dataline>();
            if (feature != null)
                com.feature = feature;

            //Draw the line
            com.Draw(line, symbology, LinePrefab, HandlePrefab, LabelPrefab, mainMat, selectedMat, lineMain, lineSelected);

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
            GameObject fs = Instantiate(HandlePrefab);
            Datapoint com = fs.GetComponent<Datapoint>();
            com.SetMaterial(mainMat, selectedMat);
            return fs;
        }

        public override void Translate(MoveArgs args)
        {
            changed = true;
            Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
            dataFeatures.ToList<Dataline>().Find(item => args.id == item.GetId()).transform.Translate(args.translate, Space.World);
        }


        public override void MoveAxis(MoveArgs args)
        {
            changed = true;
            Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
            dataFeatures.ToList<Dataline>().Find(item => args.id == item.GetId()).MoveAxisAction(args);
        }

    }
}
