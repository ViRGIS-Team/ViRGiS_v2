﻿/* MIT License

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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Virgis {
    public class FeatureAdder : MonoBehaviour {
        public GameObject blueCubePrefab;
        public GameObject defaultMarkerShape;

        private AppState _appState;
        private GameObject _markerShape;
        private Dictionary<Guid, GameObject> _markerShapeMap;

        // variables for double-press timer
        private IEnumerator _timer;
        private bool _waitingForSecondPress;

        // variables for adding feature - currently used for Line and Polygon features
        private VirgisFeature _newFeature;
        private Datapoint _firstVertex;
        private List<Datapoint> _lastVertex = new List<Datapoint>();

        // Start is called before the first frame update
        void Start() {
            _appState = AppState.instance;
            _markerShapeMap = new Dictionary<Guid, GameObject>();
            _markerShape = defaultMarkerShape;
            _appState.editSession.StartEvent.Subscribe(OnStartEditSession);
            _appState.editSession.EndEvent.Subscribe(OnEndEditSession);
            _appState.editSession.ChangeLayerEvent.Subscribe(OnEditableLayerChanged);
            _waitingForSecondPress = false;
            _newFeature = null;
        }

        void Update() {
            if (_appState.editSession.IsActive() && (_newFeature != null)) {
                MoveArgs args = new MoveArgs();
                args.pos = _markerShape.transform.position;
                _lastVertex.ForEach(dp => dp.MoveTo(args));
            }
        }

        public void LeftTriggerPressed(bool activate) {
            if (_appState.editSession.IsActive()) {
                if (_waitingForSecondPress) {
                    StopCoroutine(_timer);
                    _waitingForSecondPress = false;
                    OnTriggerDoublePress();
                } else {
                    _timer = WaitForSecondTriggerPress(_markerShape.transform.position);
                    StartCoroutine(_timer);
                    _waitingForSecondPress = true;
                }
            }
        }

        public void LeftTriggerReleased(bool activate) {
        }

        private void OnStartEditSession(bool ignore) {
            IVirgisLayer editableLayer = _appState.editSession.editableLayer;
            _markerShape = SelectMarkerShape(editableLayer);
            _markerShape.SetActive(true);
        }

        private void OnEditableLayerChanged(IVirgisLayer newEditableLayer) {
            _markerShape.SetActive(false);
            _markerShape = SelectMarkerShape(newEditableLayer);
            _markerShape.SetActive(true);
        }

        private void OnEndEditSession(bool saved) {
            _markerShape.SetActive(false);
        }

        private void OnTriggerSinglePress(Vector3 posWhenSinglePress) {
            if (_appState.editSession.IsActive()) {
                IVirgisLayer editableLayer = _appState.editSession.editableLayer;
                FeatureType dataType= editableLayer.featureType;
                Datapoint[] vertexes;
                switch (dataType) {
                    case FeatureType.POINT:
                        VirgisFeature point = editableLayer.AddFeature(new Vector3[1] { posWhenSinglePress });
                        point.UnSelected(SelectionType.SELECT);
                        break;
                    case FeatureType.LINE:
                        //Debug.Log($"ShapeAdder add Vertex");
                        if (_newFeature != null) {
                            _newFeature.AddVertex(posWhenSinglePress);
                        } else {
                            _newFeature = editableLayer.AddFeature(new Vector3[2] { posWhenSinglePress, posWhenSinglePress + Vector3.one * 0.1f });
                            // get the last vertex
                            vertexes = (_newFeature as Dataline).GetVertexes();
                            _firstVertex = vertexes[0];
                            _lastVertex.Add(vertexes[1]);
                            _firstVertex.UnSelected(SelectionType.SELECT);
                        }
                        break;
                    case FeatureType.POLYGON:
                        if (_newFeature != null) {
                            if (_lastVertex.Count == 1) {
                                _newFeature.transform.GetComponentInChildren<Dataline>().AddVertex(posWhenSinglePress);
                            } else {
                                _lastVertex[0].UnSelected(SelectionType.SELECT);
                                _lastVertex.RemoveAt(0);
                            }

                        } else {
                            _newFeature = editableLayer.AddFeature(new Vector3[4] { posWhenSinglePress, posWhenSinglePress + Vector3.right * 0.01f, posWhenSinglePress + Vector3.up * 0.01f, posWhenSinglePress });
                            vertexes = (_newFeature as Datapolygon).GetVertexes();
                            _firstVertex = vertexes[0];
                            _lastVertex.Add(vertexes[1]);
                            _lastVertex.Add(vertexes[2]);
                        }
                        break;
                }
            }
        }

        private void OnTriggerDoublePress() {
            if (_appState.editSession.IsActive()) {
                IVirgisLayer editableLayer = _appState.editSession.editableLayer;
                FeatureType dataType = editableLayer.featureType;
                switch (dataType) {
                    case FeatureType.LINE:
                        if (_newFeature != null) {
                            VirgisFeature temp = _lastVertex[0];
                            _lastVertex.Clear();
                            temp.UnSelected(SelectionType.SELECT);
                            // if edit mode is snap to anchor and start and end vertexes are at the same position
                            if (_appState.editSession.mode == EditSession.EditMode.SnapAnchor &&
                                _firstVertex.transform.position == temp.transform.position) {
                                (_newFeature as Dataline).MakeLinearRing();
                            }
                            // complete adding line feature
                            _newFeature = null;
                        }
                        break;
                    case FeatureType.POLYGON:
                        if (_newFeature != null) {
                            // complete adding polygon feature
                            _newFeature = null;
                            VirgisFeature temp = _lastVertex[0];
                            _lastVertex.Clear();
                            temp.UnSelected(SelectionType.SELECT);
                        }
                        break;
                }
            }
        }

        private GameObject SelectMarkerShape(IVirgisLayer layer) {
            if (_markerShapeMap.ContainsKey(layer.GetId())) {
                return _markerShapeMap[layer.GetId()];
            } else {
                GameObject featureShape = layer.GetFeatureShape();
                if (featureShape == null) {
                    return defaultMarkerShape;
                } else {
                    featureShape.transform.parent = transform;
                    featureShape.transform.position = defaultMarkerShape.transform.position;
                    featureShape.transform.rotation = defaultMarkerShape.transform.rotation;
                    featureShape.transform.localScale = defaultMarkerShape.transform.localScale;
                    _markerShapeMap.Add(layer.GetId(), featureShape);
                    return featureShape;
                }
            }
        }

        private IEnumerator WaitForSecondTriggerPress(Vector3 posWhenSinglePress) {
            float eventTime = Time.unscaledTime + 0.5f;
            while (Time.unscaledTime < eventTime)
                yield return null;
            _waitingForSecondPress = false;
            OnTriggerSinglePress(posWhenSinglePress);
        }

    }
}
