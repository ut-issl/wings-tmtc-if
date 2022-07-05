using System;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services.MOBC
{
  public class MobcUartTcPacketHandler : TcPacketHandlerBase, ITcPacketHandler
  {
    public MobcUartTcPacketHandler(IPortManager portManager) : base(portManager)
    {
    }

    protected override void TcPacketInfoWriteLine(TcPacketData data){
      UInt16 ccbyte1 = (UInt16)data.TcPacket[14];
      UInt16 ccbyte2 = (UInt16)data.TcPacket[15];
      UInt16 ccbyte1s = (UInt16)(ccbyte1 << 8);
      UInt16 comcode = (UInt16)(ccbyte1s + ccbyte2);
      var writtencomcode = Convert.ToString(comcode, 16);
      writtencomcode = ("0000" + writtencomcode).Substring(writtencomcode.Length + 4 - 4);
      Console.WriteLine("[TcPacket] : 0x{0:x}", writtencomcode);
    }
  }
}
