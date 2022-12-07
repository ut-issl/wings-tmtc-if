using System;
using System.Linq;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services.SECONDARY_OBC
{
  public class SecondaryObcTmPacketExtractor : TmPacketExtractorBase, ITmPacketExtractor
  {
    private static readonly byte[] STX = new byte[]{0xeb, 0x90};
    private static readonly byte[] ETX = new byte[]{0xc5, 0x79};

    public SecondaryObcTmPacketExtractor(IPortManager portManager)
      : base(portManager, new ReceivedDataConfig {
        HeaderLength = 4,
        BodyLength = 6 + 7 + 223,
        FooterLength = 4
      })
    {
    }

    protected override bool AnalyzeHeader()
    {
      if (!_buffer.GetRange(0, STX.Count()).SequenceEqual(STX))
      {
        return false;
      }
      byte[] packet_tmp = _buffer.GetRange(2, 2).ToArray();
      if (BitConverter.IsLittleEndian)
      {
        Array.Reverse(packet_tmp);
      }        
      _config.BodyLength = BitConverter.ToUInt16(packet_tmp);
            
      return true;
    }

    protected override bool AnalyzeFooter()
    {
      return _buffer.GetRange(_config.TotalLength - ETX.Count(), ETX.Count()).SequenceEqual(ETX);
    }

    protected override TmPacketData ConvertToTmPacketData(byte[] receivedDataInByteArray)
    {
      Console.WriteLine("Received");
      return base.ConvertToTmPacketData(receivedDataInByteArray);
    }
  }
}
