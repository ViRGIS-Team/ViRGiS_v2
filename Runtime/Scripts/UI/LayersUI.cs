﻿using Project;
using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Virgis
{

    /// <summary>
    /// LayersUI is the mediator for all components within the Layers UI GO (i.e. Layers Menu).
    /// </summary>
    ///
    /// For desktop Scene, the Layers UI GO is used in:
    /// 1) InputMapping
    /// 2) Menus GO
    /// 
    /// 
    public class LayersUI : MonoBehaviour
    {
        public GameObject layersScrollView;
        public GameObject layerPanelPrefab;
        public GameObject menus;

        private AppState _appState;
        private Dictionary<Guid, LayerUIPanel> _layersMap;
        private IDisposable startsub;
        private IDisposable stopsub;
        private IDisposable projsub;

        // Start is called before the first frame update
        void Start()
        {
            _appState = AppState.instance;
            startsub = _appState.editSession.StartEvent.Subscribe(OnStartEditSession);
            stopsub = _appState.editSession.EndEvent.Subscribe(OnEndEditSession);
            projsub = _appState.Project.Event.Subscribe(onProjectChange);
            _layersMap = new Dictionary<Guid, LayerUIPanel>();
            CreateLayerPanels();
        }

        private void OnDestroy() {
            startsub.Dispose();
            stopsub.Dispose();
            projsub.Dispose();
        }

        public void OnShowMenuButtonClicked()
        {
            gameObject.SetActive(false);
            menus.SetActive(true);
        }

        public void CreateLayerPanels()
        {
            // Delete any existing panel
            foreach (var panel in _layersMap) {
                Destroy(panel.Value.gameObject);
            }

            _layersMap.Clear();

            
            GameObject newLayerPanel;

            // appState.layers are actually Layer script (Component)
            AppState.instance.layers.ForEach(comp =>
            {
                // obtain the actual Layer object
                //                ILayer layer = comp.GetComponentInChildren<ILayer>();
                IVirgisLayer layer = (IVirgisLayer)comp;
                Debug.Log($"CreateLayerPanels: layer {layer.GetMetadata().Id ?? ""}, {layer.GetMetadata().DisplayName ?? ""}");
                // create a view panel for this particular layer
                newLayerPanel = (GameObject)Instantiate(layerPanelPrefab, transform);
                // obtain the panel script
                LayerUIPanel panelScript = newLayerPanel.GetComponentInChildren<LayerUIPanel>();
                // set the layer in the panel
                panelScript.layer = layer;
                // listens to panel's edit selected event
                panelScript.AddEditSelectedListener(OnLayerPanelEditSelected);
                if (layer.IsEditable()) panelScript.editLayerToggle.isOn = true;
                panelScript.viewLayerToggle.isOn = layer.IsVisible();
                // when the Layers Menu screen is first displayed,
                // edit session could already be active
                if (_appState.editSession.IsActive())
                {
                    // in edit session, layer can be set to edit
                    panelScript.editLayerToggle.interactable = true;
                }
                else
                {
                    // not in edit session, layer cannot be set to edit
                    panelScript.editLayerToggle.interactable = false;
                }
                _layersMap.Add(layer.GetId(), panelScript);
                newLayerPanel.transform.SetParent(layersScrollView.transform, false);
            });
            printEditStatus();
        }

        private void onProjectChange(GisProject project) {
            CreateLayerPanels();
        }

        private void OnStartEditSession(bool ignore)
        {
            foreach (LayerUIPanel panel in _layersMap.Values)
            {
                panel.editLayerToggle.interactable = true;
            }
        }

        private void OnEndEditSession(bool saved)
        {
            foreach (LayerUIPanel panel in _layersMap.Values)
            {
                panel.editLayerToggle.interactable = false;
            }
        }

        private void OnLayerPanelEditSelected(LayerUIPanel layerPanel, bool selected)
        {
            if (selected)
            {
                IVirgisLayer oldEditableLayer = _appState.editSession.editableLayer;
                _appState.editSession.editableLayer = layerPanel.layer;
                if (oldEditableLayer != null)
                    _layersMap[oldEditableLayer.GetId()].editLayerToggle.isOn = false;
            }
            else
            {
                IVirgisLayer oldEditableLayer = _appState.editSession.editableLayer;
                _appState.editSession.editableLayer = null;
                if (oldEditableLayer != null)
                    _layersMap[oldEditableLayer.GetId()].editLayerToggle.isOn = false;
            }
            //printEditStatus();
        }

        private void printEditStatus()
        {
            string msg = "edit status: ";
            foreach (LayerUIPanel l in _layersMap.Values)
            {
                msg += $"({l.layer.GetMetadata().Id}: {l.layer.IsEditable()}) ";
            }
            Debug.Log(msg);
        }
    }
}