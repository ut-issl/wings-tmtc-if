using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services
{
  public class OperationService : IOperationService
  {
    private readonly string _environment;
    private readonly string _connectionString;

    public OperationService(IConfiguration configuraiton)
    {
      _environment = configuraiton["WINGS:Environment"];
      _connectionString = configuraiton["WINGS:ConnectionString"];
    }

    public async Task<List<Operation>> FetchOperationAsync()
    {

      var operations =  new List<Operation>();
      var url = _connectionString + "/api/operations";

      var handler = GetHttpClientHandler();
      using (var client = new HttpClient(handler))
      {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Version = new Version(2, 0);
        var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.OK)
        {
          var jsonString = await response.Content.ReadAsStringAsync();
          var json = JsonConvert.DeserializeObject<OperationResponse>(jsonString);
          operations = json.data;
        }
      }
      return operations;
    }
    
    private HttpClientHandler GetHttpClientHandler()
    {
      var handler = new HttpClientHandler();
      switch (_environment)
      {
        case "Windows": // Windows Development Build
        case "Docker": // Linux on Docker Production Build
          handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
          break;
        
        case "Mac": // Mac Development Build
          AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
          break;
        
        default:
          throw new Exception("unsupported environment");
      }
      return handler;
    }
  }

  public class OperationResponse
  {
    public List<Operation> data { get; set; }
  }
}
