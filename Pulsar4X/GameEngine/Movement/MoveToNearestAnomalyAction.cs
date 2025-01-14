using Pulsar4X.Engine;
using Pulsar4X.JumpPoints;

namespace Pulsar4X.Movement
{
    public class MoveToNearestAnomalyAction : MoveToNearestAction
    {
        public override string Name => "Anomaly Survey Nearest";
        public override string Details => "Moves the fleet to the nearest Grav Anomaly that can be surveyed.";
        private bool GravSurveyFilter(Entity entity)
        {
            return entity.HasDataBlob<JPSurveyableDB>()
                && !entity.GetDataBlob<JPSurveyableDB>().IsSurveyComplete(RequestingFactionGuid);
        }

        public static MoveToNearestAnomalyAction CreateCommand(int factionId, Entity commandingEntity)
        {
            var command = new MoveToNearestAnomalyAction()
            {
                _entityCommanding = commandingEntity,
                UseActionLanes = true,
                RequestingFactionGuid = factionId,
                EntityCommandingGuid = commandingEntity.Id,
                EntityFactionFilter = DataStructures.EntityFilter.Neutral
            };
            command.Filter = command.GravSurveyFilter;
            return command;
        }
    }
}