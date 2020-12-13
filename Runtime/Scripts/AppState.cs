using OSGeo.OSR;
using Gdal = OSGeo.GDAL.Gdal;
using Project;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Reactive.Linq;
using OSGeo;
using Mdal;
using Pdal;


namespace Virgis {

    /// <summary>
    /// AppState is a global singleton object that stores app states, such as EditSession, etc.
    /// 
    /// Singleton pattern taken from https://learn.unity.com/tutorial/level-generation
    /// </summary>
    public class AppState : MonoBehaviour {
        public static AppState instance = null;

        private EditSession _editSession;
        private List<Component> _layers;
        private SpatialReference _crs;
        private CoordinateTransformation _trans;

        public OrientEvent Orientation {
            get;
            private set;
        }

        public InfoEvent Info {
            get;
            private set;
        }

        public ZoomEvent Zoom {
            get;
            private set;
        }

        public ButtonStatus ButtonStatus{
            get;
            private set;
        }

        /// <summary>
        /// Use this to get the project change event
        /// </summary>
        public ProjectChange Project {
            get;
            private set;
        }

        protected virtual void Awake() {
            Debug.Log("AppState awakens");
            if (instance == null) {
                Debug.Log("AppState instance assigned");
                instance = this;
            } else if (instance != this) {
                // there cannot be another instance
                Debug.Log("AppState found another instance");
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);
            _editSession = new EditSession();
            _layers = new List<Component>();
            Zoom = new ZoomEvent();
            Project = new ProjectChange();
            Info = new InfoEvent();
            ButtonStatus = new ButtonStatus();
            Orientation = new OrientEvent();

            Project.Event.Subscribe(proj => Init());
            try {
                GdalConfiguration.ConfigureOgr();
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            };
            try {
                GdalConfiguration.ConfigureGdal();
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            };
            try {
                PdalConfiguration.ConfigurePdal();
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            };
            try {
                MdalConfiguration.ConfigureMdal();
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            }
            Gdal.SetConfigOption("CURL_CA_BUNDLE", Path.Combine(Application.streamingAssetsPath, "gdal", "cacert.pem"));
        }

        /// <summary>
        /// Init is called after a project has been fully loaded.
        /// </summary>
        /// 
        /// Call this method everytime a new project has been loaded,
        /// e.g. New Project, Open Project
        public virtual void Init() {
            IVirgisLayer firstLayer = (IVirgisLayer) _layers[0];
            if (firstLayer.GetMetadata().DataType == RecordSetDataType.MapBox && _layers.Count > 1)
                firstLayer = (IVirgisLayer) _layers[1];
            firstLayer.SetEditable(true);
            _editSession.editableLayer = firstLayer;
        }

        public EditSession editSession {
            get => _editSession;
        }


        public GameObject map {
            get; set;
        }

        /// <summary>
        /// Use this to change or get the project
        /// </summary>
        public GisProject project {
            get {
                return Project.Get();
            } 
            set {
                Project.Set(value);
                initProj();
            } 
        }

        public  SpatialReference mapProj {
            get => _crs;
        }

        public CoordinateTransformation mapTrans {
            get => _trans;
        }

        /// <summary>
        /// Tasks to be run after a project is loaded
        /// </summary>
        public void initProj() {
            if (project != null) {
                _crs = new SpatialReference($@"PROJCRS[""virgis"",
                    BASEGEOGCRS[""WGS 84"",
                    DATUM[""World Geodetic System 1984"", ELLIPSOID[""WGS 84"", 6378137, 298.257223563, LENGTHUNIT[""metre"", 1]], ID[""EPSG"", 6326]], PRIMEM[""Greenwich"", 0, ANGLEUNIT[""degree"", 0.0174532925199433], ID[""EPSG"", 8901]]],
                    CONVERSION[
                        ""unknown"", METHOD[""Transverse Mercator"", ID[""EPSG"", 9807]],
                        PARAMETER
                        [""Latitude of natural origin"", {project.Origin.Coordinates.Latitude}, ANGLEUNIT[""degree"", 0.0174532925199433], ID[""EPSG"", 8801]],
                        PARAMETER
                        [""Longitude of natural origin"", {project.Origin.Coordinates.Longitude}, ANGLEUNIT[""degree"", 0.0174532925199433], ID[""EPSG"", 8802]],
                        PARAMETER[""Scale factor at natural origin"", 1, SCALEUNIT[""unity"", 1], ID[""EPSG"", 8805]],
                        PARAMETER[""False easting"", 0, LENGTHUNIT[""metre"", 1], ID[""EPSG"", 8806]],
                        PARAMETER[""False northing"", 0, LENGTHUNIT[""metre"", 1], ID[""EPSG"", 8807]]
                        ],
                        CS[Cartesian, 2],
                        AXIS[""(E)"", east, ORDER[1], LENGTHUNIT[""metre"", 1, ID[""EPSG"", 9001]]],
                        AXIS[""(N)"", north, ORDER[2], LENGTHUNIT[""metre"", 1, ID[""EPSG"", 9001]]]]");
                            }
            CoordinateTransformationOptions op = new CoordinateTransformationOptions();
            op.SetOperation("+proj=axisswap +order=1,3,2");
            _trans = new CoordinateTransformation(_crs, _crs, op);
            Zoom.Set(project.Scale);
            if (_trans == null)
                throw new NotSupportedException("transformation failed");
        }

        public List<Component> layers {
            get => _layers;
        }

        public void addLayer(Component layer) {
            _layers.Add(layer);
        }

        public void clearLayers() {
            _layers.Clear();
        }

        public Camera mainCamera {
            get; set;
        }

        public GameObject trackingSpace {
            get; set;
        }

        public bool InEditSession() {
            return _editSession.IsActive();
        }

        public void StartEditSession() {
            _editSession.Start();
        }

        public void StopSaveEditSession() {
            _editSession.StopAndSave();
        }

        public void StopDiscardEditSession() {
            _editSession.StopAndDiscard();
        }
    }
}