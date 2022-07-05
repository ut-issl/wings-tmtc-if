using System;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services
{
  public abstract class TcPacketHandlerBase
  {
    private readonly IPortManager _portManager;
    private string _opid;

    public TcPacketHandlerBase(IPortManager portManager)
    {
      _portManager = portManager;   
    }

    public void Initialize(string opid)
    {
      _opid = opid;
    }

    public string GetOpid()
    {
      return _opid;
    }

    public bool MatchesOpid(string opid)
    {
      return opid == _opid;
    }

    public virtual void HandlePacket(TcPacketData data)
    {
      Write(data.TcPacket);
      TcPacketInfoWriteLine(data);
    }

    protected void Write(byte[] packet)
    {
      _portManager.Write(packet, 0, packet.Length);
    }

    protected virtual void TcPacketInfoWriteLine(TcPacketData data)
    {
      Console.WriteLine("[TcPacket]");
    }
  }
}
