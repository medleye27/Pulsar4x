using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Orbital;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Galaxy;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Engine;

namespace Pulsar4X.Movement
{
    public class MoveToSystemBodyOrder : EntityCommand
    {
        public override string Name => $"Move to {Target.GetOwnersName()}";
        public override string Details => "Moves the fleet to the specified system body.";
        public override ActionLaneTypes ActionLanes { get; } = ActionLaneTypes.IneteractWithSelf | ActionLaneTypes.InteractWithEntitySameFleet | ActionLaneTypes.Movement;
        public override bool IsBlocking => true;

        public Entity Target { get; internal set; }

        private Entity _entityCommanding;
        internal override Entity EntityCommanding
        {
            get { return _entityCommanding; }
        }

        private List<EntityCommand> _shipCommands = new List<EntityCommand>();

        public override bool IsFinished()
        {
            return ShipsFinishedWarping();
        }

        internal override void Execute(DateTime atDateTime)
        {
            if(!IsRunning) FindColonyAndSetupWarpCommands();
        }

        private void FindColonyAndSetupWarpCommands()
        {
            if(!EntityCommanding.TryGetDatablob<FleetDB>(out var fleetDB)) return;
            if(fleetDB.FlagShipID == -1) return;

            // Get the colonies parent radius
            Target.TryGetDatablob<PositionDB>(out var targetPositionDB);

            if(targetPositionDB.OwningEntity == null) throw new NullReferenceException("targetPositionDB.OwningEntity cannot be null");

            double targetSMA = 0;

            if(Target.HasDataBlob<MassVolumeDB>())
                targetSMA = OrbitMath.LowOrbitRadius(targetPositionDB.OwningEntity);

            // Get all the ships we need to add the movement command to
            var ships = fleetDB.Children.Where(c => c.HasDataBlob<ShipInfoDB>());
            Target.TryGetDatablob<OrbitDB>(out var targetOrbitDB);

            foreach(var ship in ships)
            {
                if(!ship.HasDataBlob<WarpAbilityDB>()) continue;
                if(!ship.TryGetDatablob<PositionDB>(out var shipPositionDB)) continue;

                var shipMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;

                if(targetOrbitDB == null)
                {
                    var cmd = WarpMoveCommand.CreateCommandEZ(
                        ship,
                        Target,
                        EntityCommanding.StarSysDateTime);
                    _shipCommands.Add(cmd);
                    ship.Manager.Game.OrderHandler.HandleOrder(cmd);
                }
                else
                {
                    (Vector3 position, DateTime _) = WarpMath.GetInterceptPosition
                    (
                        ship,
                        targetPositionDB.OwningEntity.GetDataBlob<OrbitDB>(),
                        EntityCommanding.StarSysDateTime
                    );

                    Vector3 targetPos = Vector3.Normalise(position) * targetSMA;

                    // Create the movement order

                    var cmd = WarpMoveCommand.CreateCommandEZ(
                        ship,
                        Target,
                        EntityCommanding.StarSysDateTime);
                    _shipCommands.Add(cmd);
                    ship.Manager.Game.OrderHandler.HandleOrder(cmd);
                }
            }

            IsRunning = true;
        }

        private bool ShipsFinishedWarping()
        {
            if(!IsRunning) return false;

            foreach(var command in _shipCommands)
            {
                if(!command.IsFinished())
                    return false;
            }
            return true;
        }

        internal override bool IsValidCommand(Game game)
        {
            return true;
        }

        public MoveToSystemBodyOrder() { }
        public MoveToSystemBodyOrder(Entity commandingEntity, Entity targetEntity)
        {
            _entityCommanding = commandingEntity;
            Target = targetEntity;
        }

        public static MoveToSystemBodyOrder CreateCommand(int factionId, Entity commandingEntity, Entity targetEntity)
        {
            var command = new MoveToSystemBodyOrder(commandingEntity, targetEntity)
            {
                UseActionLanes = true,
                RequestingFactionGuid = factionId,
                EntityCommandingGuid = commandingEntity.Id
            };

            return command;
        }

        public override EntityCommand Clone()
        {
            var command = new MoveToSystemBodyOrder(EntityCommanding, Target)
            {
                UseActionLanes = this.UseActionLanes,
                RequestingFactionGuid = this.RequestingFactionGuid,
                EntityCommandingGuid = this.EntityCommandingGuid,
                CreatedDate = this.CreatedDate,
                ActionOnDate = this.ActionOnDate,
                ActionedOnDate = this.ActionedOnDate,
                IsRunning = this.IsRunning
            };

            return command;
        }
    }
}