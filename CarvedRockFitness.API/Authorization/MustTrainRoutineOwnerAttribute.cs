using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace CarvedRockFitness.API.Authorization
{
    public class MustTrainRoutineOwnerAttribute (string trainerRoleName, string traineeRoleName) : AuthorizeAttribute, IAuthorizationRequirement
    {
        private readonly string _trainerRoleName = trainerRoleName;
        private readonly string _traineeRoelName = traineeRoleName;
        public IEnumerable<IAuthorizationRequirement> GetRequirements() 
        {
            return [new DenyAnonymousAuthorizationRequirement(), new MustTrainRoutineOwnerRequirement(_trainerRoleName, _traineeRoelName)];
        }
    }
}
