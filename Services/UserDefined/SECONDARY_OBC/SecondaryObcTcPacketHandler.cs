using System;
using System.Linq;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services.SECONDARY_OBC
{
  public class SecondaryObcTcPacketHandler : TcPacketHandlerBase, ITcPacketHandler
  {
    public SecondaryObcTcPacketHandler(IPortManager portManager) : base(portManager)
    {
    }
  }
}
