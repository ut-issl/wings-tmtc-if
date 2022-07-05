using System;
using System.Linq;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services.MOBC
{
  public class MobcUartTmPacketExtractor : TmPacketExtractorBase, ITmPacketExtractor
  {
    //STX
    private enum Ver { Ver1 = 0b00, Ver2 = 0b01 }
    private enum ScId { SampleSat = 0x000 }
    private enum VirChId { Realtime = 0b000001, Replay = 0b000010, Fill = 0b111111 }

    //ETX
    private enum CtlWrdType { CLCW = 0b0 }
    private enum ClcwVer { Ver1 = 0b00 }
    private enum COPinEff { COP1 = 0b01 }
    private enum VCId { Default = 0b000000 }
    private enum Spare { Fixed = 0b00 }

    
    public MobcUartTmPacketExtractor(IPortManager portManager)
      : base(portManager, new ReceivedDataConfig {
        HeaderLength = 6,
        BodyLength = 434,
        FooterLength = 4
      })
    {
    }

    protected override bool AnalyzeHeader()
    {
      var STX = _buffer.GetRange(0, _config.HeaderLength).ToArray();
      //only use fixed bits
      if (!ChkVer(STX, Ver.Ver2)) { return false; } 
      // if (!ChkScId(STX, ScId.SampleSat)) { return false; } 
      return true;
    }

    protected override bool AnalyzeFooter()
    {
      var ETX = _buffer.GetRange(_config.TotalLength - _config.FooterLength,  _config.FooterLength).ToArray();
      //only use fixed bits
      if (!ChkCtlWrdType(ETX, CtlWrdType.CLCW)) { return false; } 
      if (!ChkClcwVer(ETX, ClcwVer.Ver1)) { return false; } 
      if (!ChkCOPinEff(ETX, COPinEff.COP1)) { return false; }
      if (!ChkVCId(ETX, VCId.Default)) { return false; }
      if (!ChkSpare(ETX, Spare.Fixed) ){ return false; }
      return true;
    }

    protected override TmPacketData ConvertToTmPacketData(byte[] receivedDataInByteArray)
    {
      foreach (var x in receivedDataInByteArray)
      {
        Console.Write("{0:x2} ", x);
      }
      Console.WriteLine();
      return base.ConvertToTmPacketData(receivedDataInByteArray);
    }

    private bool ChkVer(byte[] STX, Ver ver)
    {
      int pos = 0;
      byte mask = 0b_1100_0000;
      byte val = (byte)((byte)ver << 6);
      return (STX[pos] & mask) == val;
    }

    private bool ChkScId(byte[] STX, ScId id)
    {
      int pos1 = 0;
      byte mask1 = 0b_0011_1111;
      byte val1 = (byte)((byte)id >> 2);
      int pos2 = 1;
      byte mask2 = 0b_1100_0000;
      byte val2 = (byte)((byte)id << 6);
      return ((STX[pos1] & mask1) == val1) & ((STX[pos2] & mask2) == val2);
    }

    private bool ChkCtlWrdType(byte[] ETX, CtlWrdType type)
    {
      int pos = 0;
      byte mask = 0b_1000_0000;
      byte val = (byte)((byte)type << 7);
      return (ETX[pos] & mask) == val;      
    }

    private bool ChkClcwVer(byte[] ETX, ClcwVer ver)
    {
      int pos = 0;
      byte mask = 0b_0110_0000;
      byte val = (byte)((byte)ver << 5);
      return (ETX[pos] & mask) == val;      
    }

    private bool ChkCOPinEff(byte[] ETX, COPinEff eff)
    {
      int pos = 0;
      byte mask = 0b_0000_0011;
      byte val = (byte)((byte)eff);
      return (ETX[pos] & mask) == val;      
    }
    
    private bool ChkVCId(byte[] ETX, VCId id)
    {
      int pos = 1;
      byte mask = 0b_1111_1100;
      byte val = (byte)((byte)id << 2);
      return (ETX[pos] & mask) == val;      
    }
    
    private bool ChkSpare(byte[] ETX, Spare spare)
    {
      int pos = 1;
      byte mask = 0b_0000_0011;
      byte val = (byte)((byte)spare);
      return (ETX[pos] & mask) == val;      
    }
  }
}
