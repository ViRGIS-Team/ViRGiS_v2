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

using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Project;


namespace Virgis {

    public class ContainerLayer<T, S> : VirgisLayer<T, S> where T : RecordSet {

        protected new void Awake() {
            base.Awake();
            isContainer = true;
            subLayers = new List<IVirgisLayer>();
        }

        protected override Task _init() {
            return Task.CompletedTask;
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new System.NotImplementedException();
        }

        protected override void _checkpoint() {
            foreach (IVirgisLayer layer in subLayers) {
                layer.CheckPoint();
            }
        }

        protected override async Task _draw() {
            foreach (IVirgisLayer layer in subLayers) {
                await layer.Draw();
            }
            return;
        }

        protected override async Task _save() {
            foreach (IVirgisLayer layer in subLayers) {
                await layer.Save();
            }
            return;
        }
    }
}

