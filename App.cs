using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Grpc.Net.Client;
using WINGS.GrpcService;
using WINGS_TMTC_IF.Services;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF
{
  public class App
  {
    private readonly IConfiguration _configuration;
    private readonly IOperationService _operationService;
    private readonly IPortManager _portManager;
    private readonly ITmPacketExtractor _tmPacketExtractor;
    private readonly ITcPacketHandler _tcPacketHandler;
    private readonly TmtcPacketService _tmtcPacketService;
    
    public App(IConfiguration configuration,
               IOperationService operationService,
               IPortManager portManager,
               ITmPacketExtractor tmPacketExtractor,
               ITcPacketHandler tcPacketHandler,
               TmtcPacketService tmtcPacketService)
    {
      _configuration = configuration;
      _operationService = operationService;
      _portManager = portManager;
      _tmPacketExtractor = tmPacketExtractor;
      _tcPacketHandler = tcPacketHandler;
      _tmtcPacketService = tmtcPacketService;
    }
    
    public void Run()
    {
      // Select Opid
      Task<List<Operation>> opTask= Task.Run(() => _operationService.FetchOperationAsync());
      var opid = ConsoleSelectOperation(opTask.Result);
      Console.WriteLine("Operation ID: {0}\n", opid);
      
      // Initialize Port (Automatically open Serial or LAN port correspond to the selected component)
      _portManager.Initialize();

      // Initialize Packet Handler
      _tmPacketExtractor.Initialize(opid);
      _tcPacketHandler.Initialize(opid);

      // Start gRPC Service
      var client = ConfigureGrpcClient();
      var tmTask = _tmtcPacketService.TmPacketSendLoop(client);
      var tcTask = _tmtcPacketService.TcPacketReceiveLoop(client);
      Task.WaitAll(tmTask, tcTask);
      Console.ReadKey();
    }

    private string ConsoleSelectOperation(List<Operation> operations)
    {
      int num;
      Console.WriteLine("Select operation and press enter");
      for (int i = 0; i < operations.Count; i++)
      {
        var operation = operations[i];
        Console.WriteLine("[ {0} ] : {1}", i, operation.PathNumber + " " + operation.Comment);
      }
      while (true)
      {
        try
        {
          num = int.Parse(Console.ReadLine());
          var operation = operations[num];
          if (num < operations.Count)
          {
            if (!operation.IsTmtcConnected)
            {
              return operation.Id;
            }
            else
            {
              Console.WriteLine("This operation is already connected by another client");
            }
          }
          else
          {
            Console.WriteLine("Error unexpeced input");
          } 
        }
        catch
        {
          Console.WriteLine("Error unexpeced input");
        }
      }
    }

    private TmtcPacket.TmtcPacketClient ConfigureGrpcClient()
    {
      var env = _configuration["WINGS:Environment"];
      var grpcConnectionString = _configuration["WINGS:GrpcConnectionString"];
      GrpcChannel channel;

      switch (env)
      {
        case "Windows": // Windows Development Build
        case "Docker": // Linux on Docker Production Build
          var httpHandler = new HttpClientHandler();
          httpHandler.ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
          channel = GrpcChannel.ForAddress(grpcConnectionString,
            new GrpcChannelOptions { HttpHandler = httpHandler });
          break;
        
        case "Mac": // Mac Development Build
          AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
          channel = GrpcChannel.ForAddress(grpcConnectionString);
          break;
        
        default:
          throw new Exception("unsupported environment");
      }
      
      return new TmtcPacket.TmtcPacketClient(channel);
    }
  }
}
