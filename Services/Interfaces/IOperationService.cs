using System.Collections.Generic;
using System.Threading.Tasks;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services
{
  public interface IOperationService
  {
    Task<List<Operation>> FetchOperationAsync();
  }
}
