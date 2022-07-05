using System;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services.MOBC
{
  public class MobcRfTcPacketHandler : TcPacketHandlerBase, ITcPacketHandler
  {
    private const int _packetMaxLen = 1200;  // limitation from modulator spec

    public MobcRfTcPacketHandler(IPortManager portManager) : base(portManager)
    {
    }

    public override void HandlePacket(TcPacketData data)
    {
      if (data.TcPacket.Length > _packetMaxLen)
      {
        Console.Write("TcPacket size {0} byte is too large: Modulator couldn't handle it.", data.TcPacket.Length);
        return;
      }

      Write(data.TcPacket);
      TcPacketInfoWriteLine(data);
    }
  }
}
