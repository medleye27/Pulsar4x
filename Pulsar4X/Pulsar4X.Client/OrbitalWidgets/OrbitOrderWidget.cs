﻿using System;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;
using SDL2;
using System.Linq;

namespace Pulsar4X.SDL2UI
{

    /// <summary>
    /// Orbit draw data.
    /// How this works:
    /// First, we set up all the static non changing variables from the entites datablobs.
    /// On Setup we create a list of points for the full ellipse, as if it was orbiting around 0,0 world coordinates. (focalPoint).
    /// as well as the orbitAngle (Longitude of the periapsis, which should be the Argument of Periapsis + Longdidtude of the Accending Node, in 2d orbits we just add these together and use the LoP)  
    /// On Update we calculate the angle from the center of the ellipse to the orbiting entity. TODO: (this *should* only be called when the game updates, but is currently called each frame) 
    /// On Draw we translate the points to correct for the position in world view, and for the viewscreen and camera positions as well as zoom.
    /// We then find the index in the Point Array (created in Setup) that will be where the orbiting entity is, using the angle from the center of the ellipse to the orbiting entity. 
    /// Using this index we create a tempory array of only the points which will be in the drawn portion of the ellipse (UserOrbitSettings.EllipseSweepRadians) which start from where the entity should be.  
    /// We start drawing segments from where the planet will be, and decrease the alpha channel for each segment.
    /// On ajustments to settings from the user, we re-calculate needed info for that. (if the number of segments change, we have to recreate the point indiex so we run setup in that case) 
    /// Currently we're not distingishing between clockwise and counter clockwise orbits, not sure if the engine even does counterclockwise, will have to check that and fix. 
    /// </summary>
    public class OrbitOrderWidget : Icon
    {

        enum SetModes
        {
            Nothing,
            SetMajor,
            SetMinor,
            SetLoP,//Longditude of Periapsis
        }

        #region Static properties

        PositionDB _bodyPositionDB;

        internal Vector2 Apoapsis;
        internal Vector2 Periapsis;
        internal double OrbitEllipseSemiMaj_m;
        internal double OrbitEllipseSemiMinor_m;

        internal double LonditudeOfPeriapsis; //the orbit is an ellipse which is rotated arround one of the focal points. this is eqal to the LoAN + AoP 

        double _linearEccentricity_m; //distance from the center of the ellpse to one of the focal points. 

        Vector2[] _points = new Vector2[0]; //we calculate points around the ellipse and add them here. when we draw them we translate all the points. 
        SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[0];
        //sphere of influance radius, if the entity is outside this, then this is affected by the parent (or other) gravitational body
        double _soiWorldRadius_m;
        double _soiWorldRadius_AU; 
        float _soiViewRadius;
        //this is the size of the planet that we're trying to orbit, 
        //if the entity is inside this... currently nothing happens, 
        //but it shoudl be bad. we should not allow translations inside this radius, and warn if the orbit goes within this radius. 
        double _targetWorldRadius_AU;
        float _targetViewRadius;
        #endregion

        #region Dynamic Properties
        //change each game update
        float _ellipseStartArcAngleRadians;
        int _index;
        internal bool IsRetrogradeOrbit = false;
        public float EllipseSweepRadians = 4.71239f;

        //32 is a good low number, slightly ugly.  180 is a little overkill till you get really big orbits. 
        public byte NumberOfArcSegments = 180; 

        public byte Red = 0;
        public byte Grn = 255;
        public byte Blu = 0;
        public byte MaxAlpha = 255;
        public byte MinAlpha = 0; 

        //change after user makes adjustments:
        byte _numberOfArcSegments = 180; //how many segments in a complete 360 degree ellipse. this is set in UserOrbitSettings, localy adjusted because the whole point array needs re-creating when it changes. 
        int _numberOfDrawSegments; //this is now many segments get drawn in the ellipse, ie if the _ellipseSweepAngle or _numberOfArcSegments are less, less will be drawn.
        float _segmentArcSweepRadians; //how large each segment in the drawn portion of the ellipse.  
        float _alphaChangeAmount;

        double _eccentricity;
        Vector3 _position_m;

        #endregion

        #region debugData

        double _aoP = 0;
        double _loAN = 0;
        double _trueAnomaly = 0;
        #endregion

        internal OrbitOrderWidget(Entity targetEntity) : base(targetEntity.GetDataBlob<PositionDB>())
        {

            _bodyPositionDB = targetEntity.GetDataBlob<PositionDB>();

            OrbitEllipseSemiMaj_m = 20000;
            OrbitEllipseSemiMinor_m = 20000;

            _linearEccentricity_m = 0;

            _soiWorldRadius_m = targetEntity.GetSOI_m();
            _soiWorldRadius_AU = targetEntity.GetSOI_AU();
            _targetWorldRadius_AU = targetEntity.GetDataBlob<MassVolumeDB>().RadiusInAU;
            Setup();

        }

        public OrbitOrderWidget(OrbitDB orbitDB): base(orbitDB.Parent.GetDataBlob<PositionDB>())
        {
            var targetEntity = orbitDB.Parent;
            _bodyPositionDB = targetEntity.GetDataBlob<PositionDB>();

            OrbitEllipseSemiMaj_m = (float)orbitDB.SemiMajorAxis;
            _eccentricity = orbitDB.Eccentricity;
            EllipseMath.SemiMinorAxis(OrbitEllipseSemiMaj_m, _eccentricity);
            _linearEccentricity_m = (float)(orbitDB.Eccentricity * OrbitEllipseSemiMaj_m);

            _soiWorldRadius_AU = targetEntity.GetSOI_AU();
            _targetWorldRadius_AU = targetEntity.GetDataBlob<MassVolumeDB>().RadiusInAU;
            Setup();
        }


        void Setup()
        {
            _numberOfArcSegments = NumberOfArcSegments;
            CreatePointArray();

            _segmentArcSweepRadians = (float)(Math.PI * 2.0 / _numberOfArcSegments);
            _numberOfDrawSegments = (int)Math.Max(1, (EllipseSweepRadians / _segmentArcSweepRadians));
            _alphaChangeAmount = ((float)MaxAlpha - MinAlpha) / _numberOfDrawSegments;
            _ellipseStartArcAngleRadians = (float)LonditudeOfPeriapsis;



            CreatePointArray();

            OnPhysicsUpdate();

        }


        public void SetParametersFromKeplerElements(KeplerElements ke_m, Vector3 position_m)
        {

            _eccentricity = ke_m.Eccentricity;
            _linearEccentricity_m = ke_m.LinearEccentricity;
            _position_m = position_m;

            OrbitEllipseSemiMaj_m = ke_m.SemiMajorAxis; 
            OrbitEllipseSemiMinor_m = ke_m.SemiMinorAxis;

            //TODO: Periapsis and Apoapsis calc doesn't look right to me... though it's not currently being used. 
            //this was probibly written when the orbit could only be created when the ship was at the pere or apo. 
            /*
            Periapsis = new PointD()
            {
                Y = Math.Sin(ke.TrueAnomaly) * ke.Periapsis,
                X = Math.Cos(ke.TrueAnomaly) * ke.Periapsis

            };
            Apoapsis = new PointD() {
                Y = Math.Sin(ke.TrueAnomaly) * ke.Apoapsis,
                X = Math.Cos(ke.TrueAnomaly) * ke.Apoapsis
            };
            */
            if (ke_m.Inclination > Math.PI * 0.5 && ke_m.Inclination < Math.PI * 1.5) //ke inclination is in radians.
            {
                IsRetrogradeOrbit = true;
            }
            else
            {
                IsRetrogradeOrbit = false;
            }

            

            LonditudeOfPeriapsis = ke_m.LoAN + ke_m.AoP;
            _loAN = ke_m.LoAN;
            _aoP = ke_m.AoP;
            _trueAnomaly = ke_m.TrueAnomalyAtEpoch;
            OnPhysicsUpdate();
        }

        void CreatePointArray()
        {
            if(_eccentricity <1 )
                CreatePointArrayElliptic();
            else
                CreatePointArrayHyperbolic();
            
        }

        void CreatePointArrayElliptic()
        {
            var coslop = 1 * Math.Cos(LonditudeOfPeriapsis);
            var sinlop = 1 * Math.Sin(LonditudeOfPeriapsis);
            
            _points = new Vector2[_numberOfArcSegments + 1];
            double angle = 0;
            for (int i = 0; i < _numberOfArcSegments + 1; i++)
            {
                double x1 = OrbitEllipseSemiMaj_m * Math.Sin(angle) - _linearEccentricity_m; //we add the focal distance so the focal point is "center"
                double y1 = OrbitEllipseSemiMinor_m * Math.Cos(angle);

                //rotates the points to allow for the LongditudeOfPeriapsis. 
                double x2 = (x1 * coslop) - (y1 * sinlop);
                double y2 = (x1 * sinlop) + (y1 * coslop);
                _points[i] = new Vector2() { X = x2, Y = y2 };
                angle += _segmentArcSweepRadians;
            }
            
            
            if (IsRetrogradeOrbit)
            {
                var mtxr1 = Matrix3d.IDRotateZ(-LonditudeOfPeriapsis);
                var mtxri = Matrix3d.IDRotateX(Math.PI);
                var mtxr2 = Matrix3d.IDRotateZ(LonditudeOfPeriapsis);

                var mtxr = mtxr1 * mtxri * mtxr2;
                for (int i = 0; i < _points.Length; i++)
                {
                    var pnt = mtxr.Transform(new Vector3(_points[i].X, _points[i].Y, 0));
                    _points[i] = new Vector2() {X = pnt.X, Y = pnt.Y};
                }
            }
        }

        void CreatePointArrayHyperbolic()
        {
            double p = EllipseMath.SemiLatusRectum(OrbitEllipseSemiMaj_m, _eccentricity);
            double angleToSOIPoint = EllipseMath.TrueAnomalyAtRadus(_soiWorldRadius_m, p, _eccentricity);
            _points = CreatePrimitiveShapes.HyperbolicPoints(OrbitEllipseSemiMaj_m, _eccentricity, LonditudeOfPeriapsis, angleToSOIPoint, _numberOfArcSegments + 1);
        }


        public override void OnPhysicsUpdate()
        {


            CreatePointArray();
            Vector2 vector2 = new Vector2() { X = _position_m.X, Y = _position_m.Y };
 
            double minDist = (vector2 - _points[_index]).Length();

            for (int i = 0; i < _points.Count(); i++)
            {
                double dist = (vector2 - _points[i]).Length();
                if (dist < minDist)
                {
                    minDist = dist;
                    _index = i;
                }
            }
        }
        


        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {

            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition_m); 


            _soiViewRadius = camera.ViewDistance(_soiWorldRadius_AU);
            _targetViewRadius = camera.ViewDistance(_targetWorldRadius_AU);
            int index = _index;


            Vector2 translated;
            _drawPoints = new SDL.SDL_Point[_numberOfDrawSegments];
            for (int i = 0; i < _numberOfDrawSegments; i++)
            {
                if (index < _numberOfArcSegments - 1)

                    index++;
                else
                    index = 0; 
                
                    
                translated = matrix.TransformD(Distance.MToAU( _points[index].X), Distance.MToAU(_points[index].Y)); //add zoom transformation. 

                int x = (int)(ViewScreenPos.x + translated.X);
                int y = (int)(ViewScreenPos.y + translated.Y);

                _drawPoints[i] = new SDL.SDL_Point() { x = x, y = y };
            }
            
            
            
        }



        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            
            //Orbit line
            //now we draw a line between each of the points in the translatedPoints[] array.
            float alpha = MaxAlpha;
            for (int i = 0; i < _numberOfDrawSegments - 1; i++)
            {
                var au = 1;//UniversalConstants.Units.MetersPerAu;
                int x1 = (int)(_drawPoints[i].x * au);
                int y1 = (int)(_drawPoints[i].y * au);
                int x2 = (int)(_drawPoints[i + 1].x * au);
                int y2 = (int)(_drawPoints[i + 1].y * au);
                
                //SDL.SDL_RenderDrawLine(rendererPtr, x1, y1, x2, y2);

                SDL.SDL_SetRenderDrawColor(rendererPtr, Red, Grn, Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creaping up. 
                
                SDL.SDL_RenderDrawLine(rendererPtr, x1, y1, x2, y2);
                //SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[i].x, _drawPoints[i].y, _drawPoints[i + 1].x, _drawPoints[i + 1].y);
                alpha -= _alphaChangeAmount;

            }

            
            
            //SOI filled circle area. 
            SDL.SDL_SetRenderDrawColor(rendererPtr, 0, 50, 100, 100);
            //DrawPrimitive.DrawFilledCircle(rendererPtr ,ViewScreenPos.x , ViewScreenPos.y, (int)_soiViewRadius);
            //DrawPrimitive.DrawEllipse(rendererPtr, ViewScreenPos.x, ViewScreenPos.y, _soiViewRadius, _soiViewRadius);

            
            var soipnts = CreatePrimitiveShapes.BresenhamCircle(0, 0, (int)_soiViewRadius);
            
            //SDL.SDL_RenderDrawPoints(rendererPtr, soipnts.ToArray(), soipnts.Count);
            var lasty = 0;
            for (int i = 0; i < soipnts.Count ; i+=2)
            {
                var x = soipnts[i].x;
                var y = soipnts[i].y;
                if(y != lasty)
                    SDL.SDL_RenderDrawLine(rendererPtr, ViewScreenPos.x -x, ViewScreenPos.y -y, ViewScreenPos.x + x, ViewScreenPos.y - y);
                lasty = y;
            }

            
/*
            for (int i = 0; i < soipnts.Count -1; i++)
            {
                //var err = SDL.SDL_GetError();
                //SDL.SDL_RenderDrawLine(rendererPtr, soipnts[i].x, soipnts[i].y, soipnts[i + 1].x, soipnts[i + 1].y);
                if (SDL.SDL_RenderDrawPoint(rendererPtr, soipnts[i].x, soipnts[i].y) < 0)
                {
                    var err = SDL.SDL_GetError();
                }

                //SDL.SDL_RenderDrawLine(rendererPtr, ViewScreenPos.x, ViewScreenPos.y, soipnts[i].x, soipnts[i].y);
                //var err2 = SDL.SDL_GetError();
            }
  */         
            //Planet Filled Circle
            SDL.SDL_SetRenderDrawColor(rendererPtr, 100, 0, 0, 255);
            DrawPrimitive.DrawEllipse(rendererPtr, ViewScreenPos.x, ViewScreenPos.y, _targetViewRadius, _targetViewRadius);
            var plntPts = CreatePrimitiveShapes.BresenhamCircle(ViewScreenPos.x, ViewScreenPos.y, (int)_targetViewRadius);
            for (int i = 0; i < plntPts.Count -1; i++)
            {
                SDL.SDL_RenderDrawLine(rendererPtr, plntPts[i].x, plntPts[i].y, plntPts[i + 1].x, plntPts[i + 1].y);
            }

            /*
             SDL.SDL_SetRenderDrawColor(rendererPtr, 0, 100, 0, 160);
             DrawPrimitive.DrawArc(rendererPtr, ViewScreenPos.x, ViewScreenPos.y, 63, 63, 0, _loAN); //draw LoAN angle

             SDL.SDL_SetRenderDrawColor(rendererPtr, 50, 0, 100, 160);
             DrawPrimitive.DrawArc(rendererPtr, ViewScreenPos.x, ViewScreenPos.y, 64, 64, _loAN, _aoP); //draw AoP angle

             SDL.SDL_SetRenderDrawColor(rendererPtr, 100, 0, 0, 160);
             DrawPrimitive.DrawArc(rendererPtr, ViewScreenPos.x, ViewScreenPos.y, 66, 66, OrbitAngleRadians,  _trueAnomaly);
             */
        }
    }
}
