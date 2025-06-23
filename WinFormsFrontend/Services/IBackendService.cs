using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrokenStatsFrontendWinForms.Services;

public interface IBackendService
{
    Task<List<InstanceDto>> GetInstancesAsync(DateTime from, DateTime to);
    Task<List<InstanceFightDto>> GetFightsAsync(int instanceId);
}
