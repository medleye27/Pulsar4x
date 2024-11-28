using System;
using System.Collections.Generic;
using System.Drawing;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class BezierPath
    {
        List<BezierCurve> _segments;
    }

    public class BezierCurve: IDrawData
    {
        Vector2[] _controlPoints;
        List<Vector2> _linePoints;
        Vector2[] _drawPoints;
        public bool Scales = true;

        public BezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            _controlPoints = new Vector2[4] {p0, p1, p2, p3};
        }

        public void SetLinePoints(float dt)
        {
            _linePoints = new List<Vector2>();
            for (float t = 0.0f; t < 1.0; t += dt)
            {
                var x = BezCalc(t, _controlPoints[0].X, _controlPoints[1].X, _controlPoints[2].X, _controlPoints[3].X);
                var y = BezCalc(t, _controlPoints[0].Y, _controlPoints[1].Y, _controlPoints[2].Y, _controlPoints[3].Y);
                
                _linePoints.Add(new Vector2() {X = x, Y = y});
            }
        }

        private static double BezCalc(double t, double a0, double a1, double a2, double a3)
        {
            double foo = a0 * Math.Pow((1 - t), 3); 
            foo += a1 * 3 * t * Math.Pow((1 - t), 2); 
            foo += a2 * 3 * Math.Pow(t, 2) * (1 - t); 
            foo += a3 * Math.Pow(t, 3);
            return foo;
        }

        public void OnPhysicsUpdate()
        {
            
        }
        public void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            var zm =camera.GetZoomMatrix();
            //var tm = camera.GetPanMatrix();
            
            Matrix nonZoomMatrix = Matrix.IDMirror(true, false);
            var vsp = camera.ViewCoordinate_m(new Vector3(0,0,0));

            _drawPoints = new Vector2[_linePoints.Count];

            for (int i = 0; i < _linePoints.Count; i++)
            {
                var pnt = _linePoints[i];

                int x;
                int y;
                Vector2 transformedPoint;
                if (Scales)
                    transformedPoint = matrix.TransformD(pnt.X, pnt.Y); //add zoom transformation. 
                else
                    transformedPoint = nonZoomMatrix.TransformD(pnt.X, pnt.Y);

                x = (int)(vsp.x + transformedPoint.X);// + startPoint.X);
                y = (int)(vsp.y + transformedPoint.Y);// + startPoint.Y);
                _drawPoints[i] = new Vector2() { X = x, Y = y };

            }
            
            
        }



        public void Draw(IntPtr rendererPtr, Camera camera)
        {
            for (int i = 0; i < _drawPoints.Length - 1; i++)
            {
                int x0 = Convert.ToInt32(_drawPoints[i].X);
                int y0 = Convert.ToInt32(_drawPoints[i].Y);
                int x1 = Convert.ToInt32(_drawPoints[i+1].X);
                int y1 = Convert.ToInt32(_drawPoints[i+1].Y);
                SDL.SDL_RenderDrawLine(rendererPtr, x0, y0, x1, y1);
            }
            
        }
    }
}