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

using System.Collections.Generic;
using UnityEngine;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a line segment
    /// </summary>
    public class LineSegment : VirgisFeature
    {
        private Renderer thisRenderer; // convenience link to the rendere for this marker

        private Vector3 start; // coords of the start of the line in Map.local space coordinates
        private Vector3 end;  // coords of the start of the line in Map.local space coordinates
        private float diameter; // Diameter of the vertex in Map.local units
        public int vStart; // Vertex ID of the start of the lins
        public int vEnd; // Vertex ID of the end of the line
        private bool selected; //used to hold if this is a valid selection for this line segment

        /// <summary>
        /// Called to draw the line Segment 
        /// </summary>
        /// <param name="from">starting point of the line segment in worldspace coords</param>
        /// <param name="to"> end point for the line segment in worldspace coordinates</param>
        /// <param name="vertStart">vertex ID for the vertex at the start of the line segment</param>
        /// <param name="vertEnd"> vertex ID for the vertex at the end of the line segment </param>
        /// <param name="dia">Diamtere of the line segement in Map.local units</param>


        public void Draw(Vector3 from, Vector3 to, int vertStart, int vertEnd, float dia)
        {
            start = transform.parent.InverseTransformPoint(from);
            end = transform.parent.InverseTransformPoint(to);
            diameter = dia;
            vStart = vertStart;
            vEnd = vertEnd;
            _draw();

        }
        public override void Selected(SelectionType button)
        {
            if (button == SelectionType.SELECTALL) {
                transform.parent.SendMessageUpwards("Selected", button, SendMessageOptions.DontRequireReceiver);
                selected = true;
            }
        }

        public override void UnSelected(SelectionType button)
        {
            selected = false;
            if (button != SelectionType.BROADCAST)
            {
                transform.parent.SendMessageUpwards("UnSelected", button, SendMessageOptions.DontRequireReceiver);
                selected = false;
            }
        }


        public override void SetMaterial(Material mainMat, Material selectedMat) {
            this.mainMat = mainMat;
            this.selectedMat = selectedMat;
            thisRenderer = GetComponentInChildren<Renderer>();
            if (thisRenderer) {
                thisRenderer.material = mainMat;
            }
        }

        // Move the start of line to newStart point in World Coords
        public void MoveStart(Vector3 newStart)
        {
            start = transform.parent.InverseTransformPoint(newStart);
            _draw();
        }

        // Move the start of line to newStart point in World  Coords
        public void MoveEnd(Vector3 newEnd)
        {
            end = transform.parent.InverseTransformPoint(newEnd);
            _draw();
        }

        private void _draw()
        {

            transform.localPosition = start;
            transform.LookAt(transform.parent.TransformPoint(end));
            float length = Vector3.Distance(start, end) / 2.0f;
            Vector3 linescale = transform.parent.localScale;
            transform.localScale = new Vector3(diameter / linescale.x, diameter / linescale.y, length);
        }

        public override void MoveAxis(MoveArgs args){
            args.pos = transform.position;
            transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
        }

        public override void MoveTo(MoveArgs args){
            if (selected)
                SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
        }

        public override VirgisFeature AddVertex(Vector3 position) {
            GetComponentInParent<Dataline>().AddVertex( this, position);
            return this;
        }

        public void Delete() {
            transform.parent.SendMessage("RemoveVertex", this, SendMessageOptions.DontRequireReceiver);
        }

        public override Dictionary<string, object> GetInfo() {
            return GetComponentInParent<Dataline>().feature.GetAll();
        }

        public override void SetInfo(Dictionary<string, object> meta) {
            throw new System.NotImplementedException();
        }
    }
}
