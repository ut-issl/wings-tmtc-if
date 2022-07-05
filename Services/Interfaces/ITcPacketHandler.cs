using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services
{
  public interface ITcPacketHandler
  {
    void Initialize(string opid);
    string GetOpid();
    bool MatchesOpid(string opid);
    void HandlePacket(TcPacketData data);
  }
}
