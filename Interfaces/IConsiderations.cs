using Chefster.Models;
using Chefster.Common;

namespace Chefster.Interfaces;

public interface IConsiderations
{
    ServiceResult<List<ConsiderationsModel>> GetAllFamilyConsiderations(string familyId);
    ServiceResult<List<ConsiderationsModel>> GetWeeklyConsiderations(string familyId);
    ServiceResult<ConsiderationsModel> CreateConsideration(ConsiderationsCreateDto consideration);
    ServiceResult<ConsiderationsModel> DeleteConsideration(string considerationId);
    ServiceResult<ConsiderationsModel> UpdateConsideration(string considerationId, ConsiderationsUpdateDto consideration);
}
