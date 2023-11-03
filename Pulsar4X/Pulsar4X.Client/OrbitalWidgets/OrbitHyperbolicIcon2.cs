using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;
using SDL2;

namespace Pulsar4X.SDL2UI;

public class OrbitHyperbolicIcon2 : OrbitIconBase
{
    internal OrbitHyperbolicIcon2(EntityState entityState, List<List<UserOrbitSettings>> settings): base(entityState, settings)
    {

        TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Hyperbolic;
        
        UpdateUserSettings();
        CreatePointArray();
        OnPhysicsUpdate();

    }

    protected override void CreatePointArray()
    {
        
        var _soi = OrbitMath.GetSOIRadius((OrbitDB)_orbitDB.ParentDB);
        double p = EllipseMath.SemiLatusRectum(_orbitDB.SemiMajorAxis, _orbitDB.Eccentricity);
        double angleToSOIPoint = Math.Abs(EllipseMath.TrueAnomalyAtRadus(_soi, p, _orbitDB.Eccentricity));
        int retrograde = 1;
        if (IsRetrogradeOrbit)
            retrograde = -1;
        Vector2 startPos = new Vector2()
        {
            X = _soi * Math.Cos(angleToSOIPoint * retrograde),
            Y = _soi * Math.Sin(angleToSOIPoint * retrograde)
        };
        Vector2 endPos = new Vector2()
        {
            X = _soi * Math.Cos(-angleToSOIPoint * retrograde),
            Y = _soi * Math.Sin(-angleToSOIPoint * retrograde)
        };
        _points = CreatePrimitiveShapes.KeplerPoints(SemiMaj, _eccentricity, _loP_radians, startPos, endPos, _numberOfArcSegments + 1);
    }
    
    
    public override void OnPhysicsUpdate()
    {
        Vector3 pos = BodyPositionDB.RelativePosition; 
        _bodyrelativePos = new Vector2() { X = pos.X, Y = pos.Y };
        var apos = BodyPositionDB.AbsolutePosition;
        _bodyAbsolutePos = new Vector2(apos.X, apos.Y);
            
        //we find the point in the ellipse which is closest to the body so we can start drawing from the body.
        double minDist = (_bodyrelativePos - _points[_index]).Length();

        for (int i =0; i < _points.Count(); i++)
        {
            double dist = (_bodyrelativePos - _points[i]).Length();
            if (dist < minDist)
            {
                minDist = dist;
                _index = i;
            }
        }
    }
    
    public override void OnFrameUpdate(Matrix matrix, Camera camera)
    {
        //resize for zoom
        //translate to position
            
        var foo = camera.ViewCoordinateV2_m(WorldPosition_m); //camera position and zoom
            
        var trns = Matrix.IDTranslate(foo.X, foo.Y);
        var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
        var mtrx =  scAU * matrix * trns; //scale to au, scale for camera zoom, and move to camera position and zoom

        int index = _index;
        var spos = camera.ViewCoordinateV2_m(_bodyAbsolutePos);

        //_drawPoints[0] = mtrx.TransformToSDL_Point(_bodyrelativePos.X, _bodyrelativePos.Y);
        _drawPoints[0] = new SDL.SDL_Point(){x = (int)spos.X, y = (int)spos.Y};
        for (int i = 1; i < _numberOfDrawSegments; i++)
        {
            if (index < _numberOfArcSegments - 1)

                index++;
            else
                index = 0;
                
            _drawPoints[i] = mtrx.TransformToSDL_Point(_points[index].X, _points[index].Y);
        }
    }



    public override void Draw(IntPtr rendererPtr, Camera camera)
    {
        //now we draw a line between each of the points in the translatedPoints[] array.
        if (_drawPoints.Count() < _numberOfDrawSegments - 1)
            return;
        float alpha = _userSettings.MaxAlpha;
        for (int i = 0; i < _numberOfDrawSegments - 1; i++)
        {
            SDL.SDL_SetRenderDrawColor(rendererPtr, _userSettings.Red, _userSettings.Grn, _userSettings.Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creaping up. 
            SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[i].x, _drawPoints[i].y, _drawPoints[i + 1].x, _drawPoints[i +1].y);
            alpha -= _alphaChangeAmount; 
        }
    }
}