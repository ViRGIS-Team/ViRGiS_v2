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


using Project;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Virgis {


    /// <summary>
    /// This script initialises the project and loads the Project and Layer data.
    /// 
    /// It is run once at Startup
    /// </summary>
    public abstract class MapInitialize : VirgisLayer
    {
        public AppState appState;
        protected AppState m_appState;
        private ProjectJsonReader m_projectJsonReader;
        protected string m_loadOnStartup;



        ///<summary>
        ///Instantiates all singletons.
        /// </summary>
        protected new void Awake()
        {
            Debug.Log("Map awakens");
            if (AppState.instance == null) {
                Debug.Log("instantiate app state");
                m_appState = Instantiate(appState);
            }
            Debug.Log($"Virgis version : {Application.version}");
            Debug.Log($"Project version: {GisProject.GetVersion()}");
        }

        protected new void Start() {
            base.Start();
            m_appState.map = gameObject;
            Debug.Log("Checking for Startup Project");
            if (m_loadOnStartup != null)
                Load(m_loadOnStartup);
        }


        protected override Task _init() {
            throw new NotImplementedException();
        }


        /// 
        /// This is the initialisation script.
        /// 
        /// It loads the Project file, reads it for the layers and calls Draw to render each layer
        /// </summary>
        public bool Load(string file) {
            return _load(file);
        }

        protected virtual bool _load(string file) {
            Debug.Log("Starting  to load Project File");
            // Get Project definition - return if the file cannot be read - this will lead to an empty world
            m_projectJsonReader = new ProjectJsonReader();
            try {
                m_projectJsonReader.Load(file);
            } catch (Exception e) {
                Debug.LogError($"Project File {file} is invalid :: " + e.ToString());
                return false;
            }

            if (m_projectJsonReader.payload is null) {
                Debug.LogError($"Project File {file} is empty");
                return false;
            }
            m_appState.project = m_projectJsonReader.GetProject();

            try {
                   initLayers(m_appState.project.RecordSets);
            } catch (Exception e) {
                 Debug.LogError($"Project File {file} failed :" + e.ToString());
                return false;
            }
            OnLoad();
            //set globals
            m_appState.Project.Complete();
            Debug.Log("Completed load Project File");
            return true;
        }

        /// <summary>
        /// Override this call to add functionality after the Project has loaded
        /// </summary>
        public abstract void OnLoad();

        /// <summary>
        /// override this call in the consuming project to process the individual layers.
        /// This allows the consuming project to define the layer types
        /// </summary>
        /// <param name="thisLayer"> the layer that ws pulled from the project file</param>
        /// <returns></returns>
        public abstract VirgisLayer CreateLayer(RecordSet thisLayer);


        protected void initLayers(List<RecordSet> layers) {
            m_appState.tasks = new List<Coroutine>();
            foreach (RecordSet thisLayer in layers) {
                VirgisLayer temp = null;
                Debug.Log("Loading Layer : " + thisLayer.DisplayName);
                temp = CreateLayer(thisLayer);
                temp.SetMetadata(thisLayer);
                m_appState.tasks.Add(StartCoroutine(temp.Init(thisLayer).AsIEnumerator()));
            }
        }

        public void Add(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }


        /// <summary>
        /// This cll initiates the drawing of the bvirtual spce and calls `Draw ` on each layer in turn.
        /// </summary>
        new void Draw()
        {
            foreach (IVirgisLayer layer in m_appState.layers)
            {
                layer.Draw();
            }
        }

        protected override Task _draw()
        {
            throw new System.NotImplementedException();
        }

        protected override void _checkpoint()
        {
        }

        /// <summary>
        /// this call initiates the saving of the whole project and calls `Save` on each layer in turn
        /// </summary>
        /// <param name="all"></param>
        /// <returns></returns>
        public async Task<RecordSet> Save(bool all = true) {
            try {
                Debug.Log("MapInitialize.Save starts");
                if (m_appState.project != null) {
                    if (all) {
                        foreach (IVirgisLayer com in m_appState.layers) {
                            RecordSet alayer = await com.Save();
                            int index = m_appState.project.RecordSets.FindIndex(x => x.Id == alayer.Id);
                            m_appState.project.RecordSets[index] = alayer;
                        }
                    }
                    m_appState.project.Scale[m_appState.currentView] = m_appState.Zoom.Get();
                    m_appState.project.Cameras[m_appState.currentView] = m_appState.mainCamera.transform.position.ToPoint();
                    m_projectJsonReader.SetProject(m_appState.project);
                    await m_projectJsonReader.Save();
                }
                return default;
            } catch (Exception e) {
                Debug.Log("Save failed : " + e.ToString());
                return default;
            }
        }

        protected override Task _save()
        {
            throw new System.NotImplementedException();
        }


        protected override void _onEditStart(bool ignore)
        {
            CheckPoint();
        }

        /// <summary>
        /// Called when an edit session ends
        /// </summary>
        /// <param name="saved">true if stop and save, false if stop and discard</param>
        protected async override void _onEditStop(bool saved)
        {
            if (!saved) {
                Draw();
            }
            await Save(saved);
    }

        public override GameObject GetFeatureShape()
        {
            return null;
        }

    }
}
