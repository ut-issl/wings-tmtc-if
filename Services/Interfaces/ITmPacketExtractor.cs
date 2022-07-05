using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services
{
  public interface ITmPacketExtractor
  {
    void Initialize(string opid);
    TmPacketData Dequeue();
    bool PacketQueueExists();
  }
}
