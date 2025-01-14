using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Interfaces;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;


public class PositionDB : TreeHierarchyDB, IPosition
{
    /// <summary>
    /// Most objects should have a movetype. none should be used in rare occasions eg anomalies/jump points.
    /// ships at None type objects should remain warping with a speed of zero, but be using warp resources.
    /// </summary>
    public enum MoveTypes
    {
        None,
        Orbit,
        NewtonSimple,
        NewtonComplex,
        Warp,
    }

    public MoveTypes MoveType { get; internal set; }

    public KeplerElements GetKeplerElements { get; internal set; }

    public Vector3 RelativePosition { get; internal set; }

    public Vector2 RelativePosition2
    {
        get { return (Vector2)RelativePosition; }
        set { RelativePosition = (Vector3)value; }
    }
    public Vector3 AbsolutePosition
    {
        get
        {
            if ( Parent == null || !Parent.IsValid ) //migth be better than crashing if parent is suddenly not valid. should be handled before this though.
                return RelativePosition;
            else if (Parent == OwningEntity)
                throw new Exception("Infinite loop triggered");
            else
            {
                PositionDB? parentpos = (PositionDB?)ParentDB;
                if(parentpos == this)
                    throw new Exception("Infinite loop triggered");
                return parentpos.AbsolutePosition + RelativePosition;
            }
        }
        internal set
        {
            if (Parent == null)
                RelativePosition = value;
            else
            {
                PositionDB? parentpos = (PositionDB?)ParentDB;
                RelativePosition = value - parentpos.AbsolutePosition;
            }
        }
    }

    public Vector2 AbsolutePosition2     {
        get { return (Vector2)AbsolutePosition; }
        set { AbsolutePosition = (Vector3)value; }
    }

    public Vector2 Velocity { get; internal set; }

    public double SGP { get; internal set; }


    /// <summary>
    /// Initialized
    /// .
    /// </summary>
    /// <param name="x">X value.</param>
    /// <param name="y">Y value.</param>
    /// <param name="z">Z value.</param>
    public PositionDB(double x, double y, double z, Entity? parent = null) : base(parent)
    {
        AbsolutePosition = new Vector3(x, y, z);
        SetParent(parent);
        //SystemGuid = systemGuid;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="relativePos_m"></param>
    /// <param name="systemGuid"></param>
    /// <param name="parent"></param>
    public PositionDB(Vector3 relativePos, Entity? parent = null) : base(parent)
    {
        SetParent(parent);
        RelativePosition = relativePos;

    }

    public PositionDB(Entity? parent = null) : base(parent)
    {
        Vector3? parentPos = (ParentDB as PositionDB)?.AbsolutePosition;
        AbsolutePosition = parentPos ?? Vector3.Zero;
    }

    public PositionDB(PositionDB positionDB)
        : base(positionDB.Parent)
    {
        RelativePosition = positionDB.RelativePosition;

    }

    public override object Clone()
    {
        return new PositionDB(this);
    }

    //[UsedImplicitly]

    /// <summary>
    /// changes the positions relative to
    /// Can be null.
    /// </summary>
    /// <param name="newParent"></param>
    internal override void SetParent(Entity? newParent)
    {
        if (newParent != null && !newParent.HasDataBlob<PositionDB>())
            throw new Exception("newParent must have a PositionDB");
        var oldParent = ParentDB;


        Vector3 currentAbsolute = this.AbsolutePosition;
        Vector3 newRelative;
        if (newParent == null)
        {
            newRelative = currentAbsolute;
            SGP = double.PositiveInfinity;
        }
        else
        {
            newRelative = currentAbsolute - newParent.GetDataBlob<PositionDB>().AbsolutePosition;
            var mass = newParent.GetDataBlob<MassVolumeDB>().MassTotal;
            if(OwningEntity != null)
                mass += _owningEntity_.GetDataBlob<MassVolumeDB>().MassTotal;
            SGP = GeneralMath.StandardGravitationalParameter(mass);
        }
        base.SetParent(newParent);
        RelativePosition = newRelative;

    }
}

public class MoveStateProcessor : IInstanceProcessor
{
    public void Init(Game game)
    {

    }

    public static void ProcessForType(List<OrbitDB> orbits, DateTime atDateTime)
    {
        foreach (var orbitDB in orbits)
        {
            if(orbitDB.OwningEntity is not null)
                ProcessForType(orbitDB, atDateTime);
        }
    }

    public static void ProcessForType(OrbitDB orbitDB, DateTime atDateTime)
    {
        if(orbitDB.OwningEntity is null)
            return;
        if(!orbitDB.OwningEntity.TryGetDatablob(out PositionDB stateDB))
        {
            stateDB = new PositionDB(orbitDB.Parent);
            orbitDB.OwningEntity.SetDataBlob(stateDB);
        }

        stateDB.MoveType = PositionDB.MoveTypes.Orbit;
        stateDB.SetParent(orbitDB.Parent);
        stateDB.SGP = orbitDB.GravitationalParameter_m3S2;
        stateDB.GetKeplerElements = orbitDB.GetElements();
        stateDB.RelativePosition2 = orbitDB._position; //(Vector2)orbitDB.OwningEntity.GetDataBlob<PositionDB>().RelativePosition;
        orbitDB.OwningEntity.GetDataBlob<PositionDB>().RelativePosition = (Vector3)orbitDB._position;
        stateDB.Velocity = (Vector2)orbitDB.InstantaneousOrbitalVelocityVector_m(atDateTime);
    }

    public static void ProcessForType(List<OrbitUpdateOftenDB> orbits, DateTime atDateTime)
    {
        foreach (var orbitDB in orbits)
        {
            if(orbitDB.OwningEntity is not null)
                ProcessForType(orbitDB, atDateTime);
        }
    }

    public static void ProcessForType(OrbitUpdateOftenDB orbitDB, DateTime atDateTime)
    {
        if(orbitDB.OwningEntity is null)
            return;
        if(!orbitDB.OwningEntity.TryGetDatablob(out PositionDB stateDB))
        {
            stateDB = new PositionDB(orbitDB.Parent);
            orbitDB.OwningEntity.SetDataBlob(stateDB);
        }

        stateDB.MoveType = PositionDB.MoveTypes.Orbit;
        stateDB.SetParent(orbitDB.Parent);
        stateDB.SGP = orbitDB.GravitationalParameter_m3S2;
        stateDB.GetKeplerElements = orbitDB.GetElements();
        stateDB.RelativePosition2 = orbitDB._position;
        orbitDB.OwningEntity.GetDataBlob<PositionDB>().RelativePosition = (Vector3)orbitDB._position;
        stateDB.Velocity = (Vector2)orbitDB.InstantaneousOrbitalVelocityVector_m(atDateTime);
    }

    public static void ProcessForType(List<NewtonSimpleMoveDB> moves, DateTime atDateTime)
    {
        foreach (var movedb in moves)
        {
            if(movedb.OwningEntity is null)
                continue;
            if(!movedb.OwningEntity.TryGetDatablob(out PositionDB stateDB))
            {
                stateDB = new PositionDB(movedb.SOIParent);
                movedb.OwningEntity.SetDataBlob(stateDB);
            }

            stateDB.MoveType = PositionDB.MoveTypes.NewtonSimple;
            stateDB.SetParent(movedb.SOIParent);
            var myMass = movedb.OwningEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            var pMass = movedb.SOIParent.GetDataBlob<MassVolumeDB>().MassTotal;
            stateDB.SGP = GeneralMath.StandardGravitationalParameter(myMass + pMass);
            var state = OrbitMath.GetStateVectors(movedb.CurrentTrajectory, atDateTime);
            stateDB.RelativePosition = state.position;
            stateDB.Velocity = state.velocity;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(stateDB.SGP, state.position, (Vector3)state.velocity, atDateTime);
            stateDB.GetKeplerElements = ke;
        }
    }
    public static void ProcessForType(NewtonSimpleMoveDB movedb, DateTime atDateTime)
    {
        if (movedb.OwningEntity is null)
            return;
        if(!movedb.OwningEntity.TryGetDatablob(out PositionDB stateDB))
        {
            stateDB = new PositionDB(movedb.SOIParent);
            movedb.OwningEntity.SetDataBlob(stateDB);
        }

        stateDB.MoveType = PositionDB.MoveTypes.NewtonSimple;
        stateDB.SetParent(movedb.SOIParent);
        var myMass = movedb.OwningEntity.GetDataBlob<MassVolumeDB>().MassTotal;
        var pMass = movedb.SOIParent.GetDataBlob<MassVolumeDB>().MassTotal;
        stateDB.SGP = GeneralMath.StandardGravitationalParameter(myMass + pMass);
        var state = OrbitMath.GetStateVectors(movedb.CurrentTrajectory, atDateTime);
        stateDB.RelativePosition = state.position;
        stateDB.Velocity = state.velocity;
        var ke = OrbitMath.KeplerFromPositionAndVelocity(stateDB.SGP, state.position, (Vector3)state.velocity, atDateTime);
        stateDB.GetKeplerElements = ke;
    }

    public static void ProcessForType(List<NewtonMoveDB> moves, DateTime atDateTime)
    {
        foreach (var movedb in moves)
        {
            if(movedb.OwningEntity is not null)
                ProcessForType(movedb, atDateTime);
        }
    }

    public static void ProcessForType(NewtonMoveDB movedb, DateTime atDateTime)
    {
        if(movedb.OwningEntity is null)
            return;
        if(!movedb.OwningEntity.TryGetDatablob(out PositionDB stateDB))
        {
            stateDB = new PositionDB(movedb.SOIParent);
            movedb.OwningEntity.SetDataBlob(stateDB);
        }

        stateDB.MoveType = PositionDB.MoveTypes.NewtonSimple;
        stateDB.SetParent(movedb.SOIParent);
        stateDB.GetKeplerElements = movedb.GetElements();
        stateDB.SGP = stateDB.GetKeplerElements.StandardGravParameter;
        //newtonmove processor still updates positon in the processor.
        //stateDB.RelativePosition = (Vector2)movedb.OwningEntity.GetDataBlob<PositionDB>().RelativePosition;
        stateDB.Velocity = (Vector2)movedb.CurrentVector_ms;
    }

    public static void ProcessForType(List<WarpMovingDB> warps, DateTime atDateTime)
    {
        foreach (var warpdb in warps)
        {

            if(warpdb.OwningEntity is not null)
                ProcessForType(warpdb, atDateTime);
        }
    }

    public static void ProcessForType(WarpMovingDB warpdb, DateTime atDateTime)
    {
        if(warpdb.OwningEntity is null)
            return;
        if(!warpdb.OwningEntity.TryGetDatablob(out PositionDB stateDB))
        {
            stateDB = new PositionDB(warpdb._parentEnitity);
            warpdb.OwningEntity.SetDataBlob(stateDB);
        }

        stateDB.MoveType = PositionDB.MoveTypes.Warp;

        stateDB.SetParent(warpdb._parentEnitity);
        stateDB.GetKeplerElements = warpdb.EndpointTargetOrbit;
        stateDB.SGP = stateDB.GetKeplerElements.StandardGravParameter;
        stateDB.RelativePosition2 = warpdb._position;
        stateDB.Velocity = (Vector2)warpdb.CurrentNonNewtonionVectorMS;
        stateDB.OwningEntity.GetDataBlob<PositionDB>().RelativePosition = (Vector3)warpdb._position;
    }

    public Type GetParameterType => typeof(PositionDB);
    internal override void ProcessEntity(Entity entity, DateTime atDateTime)
    {

        if(entity.TryGetDatablob(out OrbitDB odb))
            ProcessForType(odb, atDateTime);
        else if(entity.TryGetDatablob(out OrbitUpdateOftenDB oudb))
            ProcessForType(oudb, atDateTime);
        else if(entity.TryGetDatablob(out NewtonMoveDB mdb))
            ProcessForType(mdb, atDateTime);
        else if(entity.TryGetDatablob(out NewtonSimpleMoveDB nmdb))
            ProcessForType(nmdb, atDateTime);
        else if(entity.TryGetDatablob(out NewtonSimpleMoveDB warpdb))
            ProcessForType(warpdb, atDateTime);
    }

    /// <summary>
    /// This allows easy single entity move processing regardless of move type.
    /// this should only be used in rare cases where you need to update a position ouside of the normal move tick.
    /// this WILL update the position, to get the position without updating, use GetFuturePosition() instead.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="toDateTime"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal static void ProcessEntityMove(Entity entity, DateTime toDateTime)
    {
        var movestate = entity.GetDataBlob<PositionDB>();
        switch (movestate.MoveType)
        {
            case PositionDB.MoveTypes.None:
            {
                break;
            }
            case PositionDB.MoveTypes.Orbit:
            {
                OrbitProcessor.ProcessEntity(entity, toDateTime);
                break;
            }
            case PositionDB.MoveTypes.NewtonSimple:
            {
                NewtonSimpleProcessor.ProcessEntity(entity, toDateTime);
                break;
            }
            case PositionDB.MoveTypes.NewtonComplex:
            {
                NewtonionMovementProcessor.ProcessEntity(entity, toDateTime);
                break;
            }
            case PositionDB.MoveTypes.Warp:
            {
                WarpMoveProcessor.ProcessEntity(entity, toDateTime);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}