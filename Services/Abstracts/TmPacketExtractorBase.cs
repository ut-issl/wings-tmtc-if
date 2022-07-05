using System;
using System.Collections.Generic;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services
{
  public abstract class TmPacketExtractorBase
  {
    private readonly IPortManager _portManager;
    protected Queue<TmPacketData> _packetQueue;
    protected ReceivedDataConfig _config;
    protected string _opid;
    protected List<byte> _buffer;
    
    public TmPacketExtractorBase(IPortManager portManager,
                             ReceivedDataConfig config)
    {
      _portManager = portManager;
      _config = config;
      _packetQueue = new Queue<TmPacketData>();
      _buffer = new List<byte>();
    }

    public virtual void Initialize(string opid)
    {
      _opid = opid;
      _portManager.NewDataReceived += NewDataHandle;
    }

    protected virtual void NewDataHandle(object sender, DataEventArgs e)
    {
      _buffer.AddRange(e.Data);
      while (_buffer.Count >= _config.HeaderLength)
      {
        if (AnalyzeHeader())
        {
          if (_buffer.Count >= _config.TotalLength)
          {
            if (AnalyzeFooter())
            {
              var receivedDataInByteArray = _buffer.GetRange(0, _config.TotalLength).ToArray();
              var data = ConvertToTmPacketData(receivedDataInByteArray);
              _packetQueue.Enqueue(data);
              _buffer.RemoveRange(0, _config.TotalLength);
            }
            else
            {
              // トータルサイズ以上あるのにフッタを解釈できない
              RemoveAllData();
            }
          }
          else
          {
            break;
          }
        }
        else
        {
          // ヘッダ長以上あるのにヘッダを解釈できない
          RemoveAllData();
        }
      }
    }

    protected abstract bool AnalyzeHeader();
    
    protected abstract bool AnalyzeFooter();

    protected virtual TmPacketData ConvertToTmPacketData(byte[] receivedDataInByteArray)
    {
      return new TmPacketData{
        Opid = _opid,
        TmPacket = receivedDataInByteArray
      };
    }

    protected virtual void RemoveAllData()
    {
      _buffer.Clear();
      Console.WriteLine("Remove garbage data");
    }

    public TmPacketData Dequeue()
    {
      return _packetQueue.Dequeue();
    }

    public bool PacketQueueExists()
    {
      return _packetQueue.Count != 0;
    }
  }
}
