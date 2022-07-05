using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Google.Protobuf;
using WINGS.GrpcService;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services
{
  public class TmtcPacketService
  {
    private readonly ITmPacketExtractor _tmPacketExtractor;
    private readonly ITcPacketHandler _tcPacketHandler;

    public TmtcPacketService(ITmPacketExtractor tmPacketExtractor,
                             ITcPacketHandler tcPacketHandler)
    {
      _tmPacketExtractor = tmPacketExtractor;
      _tcPacketHandler = tcPacketHandler;
    }

    public async Task TmPacketSendLoop(TmtcPacket.TmtcPacketClient client)
    {
      while (true)
      { 
        if (_tmPacketExtractor.PacketQueueExists())
        {
          var dataRpc = ToRpcModel(_tmPacketExtractor.Dequeue());
          var response = await client.TmPacketTransferAsync(dataRpc);
          Console.WriteLine("[TmPacketAck]: " + response.Ack.ToString());
        }
        await Task.Delay(10);
      }
    }

    public async Task TcPacketReceiveLoop(TmtcPacket.TmtcPacketClient client)
    {
      Console.WriteLine("[TcPacket] : Request");
      var result = client.TcPacketTransfer(new TcPacketRequestRpc
      {
        Opid = _tcPacketHandler.GetOpid()
      });
      var tokenSource = new CancellationTokenSource();
      try
      {
        await foreach (var data in result.ResponseStream.ReadAllAsync(tokenSource.Token))
        {
          if (_tcPacketHandler.MatchesOpid(data.Opid))
          { 
            _tcPacketHandler.HandlePacket(FromRpcModel(data));
          }
          else
          {
            Console.Write("Error operation id doesn't match");
          }
        }
      }
      catch (RpcException e)
      {
        Console.WriteLine(e.ToString());
      }
    }

    private TmPacketDataRpc ToRpcModel(TmPacketData data)
    {
      return new TmPacketDataRpc{
        Opid = data.Opid,
        TmPacket = ByteString.CopyFrom(data.TmPacket)
      };
    }

    private TcPacketData FromRpcModel(TcPacketDataRpc dataRpc)
    {
      return new TcPacketData{
        Opid = dataRpc.Opid,
        TcPacket = dataRpc.TcPacket.ToByteArray()
      };
    }
  }
}
