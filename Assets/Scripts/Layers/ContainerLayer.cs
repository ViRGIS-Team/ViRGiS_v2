﻿
using UnityEngine;
using System.Threading.Tasks;

namespace Virgis {


    public class ContainerLayer : VirgisLayer {

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new System.NotImplementedException();
        }

        protected override void _checkpoint() {
            VirgisLayer[] layers = GetComponentsInChildren<VirgisLayer>();
            foreach (VirgisLayer layer in layers) {
                layer.CheckPoint();
            }
        }

        protected override void _draw() {
            VirgisLayer[] layers = GetComponents<VirgisLayer>();
            foreach (VirgisLayer layer in layers) {
                layer.Draw();
            }
        }

        protected override Task _save() {
            VirgisLayer[] layers = GetComponentsInChildren<VirgisLayer>();
            foreach (VirgisLayer layer in layers) {
                layer.Save();
            }
            return Task.CompletedTask;
        }
    }
}

