namespace WINGS_TMTC_IF.Services.IsslCommon
{
  public class IsslCommonTcPacketHandler : TcPacketHandlerBase, ITcPacketHandler
  {
    public IsslCommonTcPacketHandler(IPortManager portManager) : base(portManager)
    {
    }
  }
}
