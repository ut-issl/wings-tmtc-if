using System;
using System.Linq;
using System.Collections.Generic;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services.MOBC
{
  public class MobcRfTmPacketExtractor : TmPacketExtractorBase, ITmPacketExtractor
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

    //Reed-Solomon decoding
    private const int ReedSolomonCodeLength = 64;  //TODO: 他のインターリーブ深度にも対応するか？
    private const int MM = 8;
    private const int NN = 255;
    private const int NROOTS = 32;
    private const int FCR = 112;
    private const int PRIM = 11;
    private const int IPRIM = 116;
    private const int A0 = NN;
    private static readonly byte[] CCSDS_alpha_to = {
      0x01,0x02,0x04,0x08,0x10,0x20,0x40,0x80,0x87,0x89,0x95,0xad,0xdd,0x3d,0x7a,0xf4,
      0x6f,0xde,0x3b,0x76,0xec,0x5f,0xbe,0xfb,0x71,0xe2,0x43,0x86,0x8b,0x91,0xa5,0xcd,
      0x1d,0x3a,0x74,0xe8,0x57,0xae,0xdb,0x31,0x62,0xc4,0x0f,0x1e,0x3c,0x78,0xf0,0x67,
      0xce,0x1b,0x36,0x6c,0xd8,0x37,0x6e,0xdc,0x3f,0x7e,0xfc,0x7f,0xfe,0x7b,0xf6,0x6b,
      0xd6,0x2b,0x56,0xac,0xdf,0x39,0x72,0xe4,0x4f,0x9e,0xbb,0xf1,0x65,0xca,0x13,0x26,
      0x4c,0x98,0xb7,0xe9,0x55,0xaa,0xd3,0x21,0x42,0x84,0x8f,0x99,0xb5,0xed,0x5d,0xba,
      0xf3,0x61,0xc2,0x03,0x06,0x0c,0x18,0x30,0x60,0xc0,0x07,0x0e,0x1c,0x38,0x70,0xe0,
      0x47,0x8e,0x9b,0xb1,0xe5,0x4d,0x9a,0xb3,0xe1,0x45,0x8a,0x93,0xa1,0xc5,0x0d,0x1a,
      0x34,0x68,0xd0,0x27,0x4e,0x9c,0xbf,0xf9,0x75,0xea,0x53,0xa6,0xcb,0x11,0x22,0x44,
      0x88,0x97,0xa9,0xd5,0x2d,0x5a,0xb4,0xef,0x59,0xb2,0xe3,0x41,0x82,0x83,0x81,0x85,
      0x8d,0x9d,0xbd,0xfd,0x7d,0xfa,0x73,0xe6,0x4b,0x96,0xab,0xd1,0x25,0x4a,0x94,0xaf,
      0xd9,0x35,0x6a,0xd4,0x2f,0x5e,0xbc,0xff,0x79,0xf2,0x63,0xc6,0x0b,0x16,0x2c,0x58,
      0xb0,0xe7,0x49,0x92,0xa3,0xc1,0x05,0x0a,0x14,0x28,0x50,0xa0,0xc7,0x09,0x12,0x24,
      0x48,0x90,0xa7,0xc9,0x15,0x2a,0x54,0xa8,0xd7,0x29,0x52,0xa4,0xcf,0x19,0x32,0x64,
      0xc8,0x17,0x2e,0x5c,0xb8,0xf7,0x69,0xd2,0x23,0x46,0x8c,0x9f,0xb9,0xf5,0x6d,0xda,
      0x33,0x66,0xcc,0x1f,0x3e,0x7c,0xf8,0x77,0xee,0x5b,0xb6,0xeb,0x51,0xa2,0xc3,0x00,
    };
    private static readonly int[] CCSDS_index_of = {
      255,  0,  1, 99,  2,198,100,106,  3,205,199,188,101,126,107, 42,
        4,141,206, 78,200,212,189,225,102,221,127, 49,108, 32, 43,243,
        5, 87,142,232,207,172, 79,131,201,217,213, 65,190,148,226,180,
      103, 39,222,240,128,177, 50, 53,109, 69, 33, 18, 44, 13,244, 56,
        6,155, 88, 26,143,121,233,112,208,194,173,168, 80,117,132, 72,
      202,252,218,138,214, 84, 66, 36,191,152,149,249,227, 94,181, 21,
      104, 97, 40,186,223, 76,241, 47,129,230,178, 63, 51,238, 54, 16,
      110, 24, 70,166, 34,136, 19,247, 45,184, 14, 61,245,164, 57, 59,
        7,158,156,157, 89,159, 27,  8,144,  9,122, 28,234,160,113, 90,
      209, 29,195,123,174, 10,169,145, 81, 91,118,114,133,161, 73,235,
      203,124,253,196,219, 30,139,210,215,146, 85,170, 67, 11, 37,175,
      192,115,153,119,150, 92,250, 82,228,236, 95, 74,182,162, 22,134,
      105,197, 98,254, 41,125,187,204,224,211, 77,140,242, 31, 48,220,
      130,171,231, 86,179,147, 64,216, 52,176,239, 38, 55, 12, 17, 68,
      111,120, 25,154, 71,116,167,193, 35, 83,137,251, 20, 93,248,151,
       46, 75,185, 96, 15,237, 62,229,246,135,165, 23, 58,163, 60,183,
    };
    private static readonly int[] CCSDS_poly = {
       0,249, 59, 66,  4, 43,126,251, 97, 30,  3,213, 50, 66,170,  5,
      24,  5,170, 66, 50,213,  3, 30, 97,251,126, 43,  4, 66, 59,249,
       0,
    };
    private static readonly byte[] Taltab = {
      0x00,0x7b,0xaf,0xd4,0x99,0xe2,0x36,0x4d,0xfa,0x81,0x55,0x2e,0x63,0x18,0xcc,0xb7,
      0x86,0xfd,0x29,0x52,0x1f,0x64,0xb0,0xcb,0x7c,0x07,0xd3,0xa8,0xe5,0x9e,0x4a,0x31,
      0xec,0x97,0x43,0x38,0x75,0x0e,0xda,0xa1,0x16,0x6d,0xb9,0xc2,0x8f,0xf4,0x20,0x5b,
      0x6a,0x11,0xc5,0xbe,0xf3,0x88,0x5c,0x27,0x90,0xeb,0x3f,0x44,0x09,0x72,0xa6,0xdd,
      0xef,0x94,0x40,0x3b,0x76,0x0d,0xd9,0xa2,0x15,0x6e,0xba,0xc1,0x8c,0xf7,0x23,0x58,
      0x69,0x12,0xc6,0xbd,0xf0,0x8b,0x5f,0x24,0x93,0xe8,0x3c,0x47,0x0a,0x71,0xa5,0xde,
      0x03,0x78,0xac,0xd7,0x9a,0xe1,0x35,0x4e,0xf9,0x82,0x56,0x2d,0x60,0x1b,0xcf,0xb4,
      0x85,0xfe,0x2a,0x51,0x1c,0x67,0xb3,0xc8,0x7f,0x04,0xd0,0xab,0xe6,0x9d,0x49,0x32,
      0x8d,0xf6,0x22,0x59,0x14,0x6f,0xbb,0xc0,0x77,0x0c,0xd8,0xa3,0xee,0x95,0x41,0x3a,
      0x0b,0x70,0xa4,0xdf,0x92,0xe9,0x3d,0x46,0xf1,0x8a,0x5e,0x25,0x68,0x13,0xc7,0xbc,
      0x61,0x1a,0xce,0xb5,0xf8,0x83,0x57,0x2c,0x9b,0xe0,0x34,0x4f,0x02,0x79,0xad,0xd6,
      0xe7,0x9c,0x48,0x33,0x7e,0x05,0xd1,0xaa,0x1d,0x66,0xb2,0xc9,0x84,0xff,0x2b,0x50,
      0x62,0x19,0xcd,0xb6,0xfb,0x80,0x54,0x2f,0x98,0xe3,0x37,0x4c,0x01,0x7a,0xae,0xd5,
      0xe4,0x9f,0x4b,0x30,0x7d,0x06,0xd2,0xa9,0x1e,0x65,0xb1,0xca,0x87,0xfc,0x28,0x53,
      0x8e,0xf5,0x21,0x5a,0x17,0x6c,0xb8,0xc3,0x74,0x0f,0xdb,0xa0,0xed,0x96,0x42,0x39,
      0x08,0x73,0xa7,0xdc,0x91,0xea,0x3e,0x45,0xf2,0x89,0x5d,0x26,0x6b,0x10,0xc4,0xbf,
    };
    private static readonly byte[] Tal1tab = {
      0x00,0xcc,0xac,0x60,0x79,0xb5,0xd5,0x19,0xf0,0x3c,0x5c,0x90,0x89,0x45,0x25,0xe9,
      0xfd,0x31,0x51,0x9d,0x84,0x48,0x28,0xe4,0x0d,0xc1,0xa1,0x6d,0x74,0xb8,0xd8,0x14,
      0x2e,0xe2,0x82,0x4e,0x57,0x9b,0xfb,0x37,0xde,0x12,0x72,0xbe,0xa7,0x6b,0x0b,0xc7,
      0xd3,0x1f,0x7f,0xb3,0xaa,0x66,0x06,0xca,0x23,0xef,0x8f,0x43,0x5a,0x96,0xf6,0x3a,
      0x42,0x8e,0xee,0x22,0x3b,0xf7,0x97,0x5b,0xb2,0x7e,0x1e,0xd2,0xcb,0x07,0x67,0xab,
      0xbf,0x73,0x13,0xdf,0xc6,0x0a,0x6a,0xa6,0x4f,0x83,0xe3,0x2f,0x36,0xfa,0x9a,0x56,
      0x6c,0xa0,0xc0,0x0c,0x15,0xd9,0xb9,0x75,0x9c,0x50,0x30,0xfc,0xe5,0x29,0x49,0x85,
      0x91,0x5d,0x3d,0xf1,0xe8,0x24,0x44,0x88,0x61,0xad,0xcd,0x01,0x18,0xd4,0xb4,0x78,
      0xc5,0x09,0x69,0xa5,0xbc,0x70,0x10,0xdc,0x35,0xf9,0x99,0x55,0x4c,0x80,0xe0,0x2c,
      0x38,0xf4,0x94,0x58,0x41,0x8d,0xed,0x21,0xc8,0x04,0x64,0xa8,0xb1,0x7d,0x1d,0xd1,
      0xeb,0x27,0x47,0x8b,0x92,0x5e,0x3e,0xf2,0x1b,0xd7,0xb7,0x7b,0x62,0xae,0xce,0x02,
      0x16,0xda,0xba,0x76,0x6f,0xa3,0xc3,0x0f,0xe6,0x2a,0x4a,0x86,0x9f,0x53,0x33,0xff,
      0x87,0x4b,0x2b,0xe7,0xfe,0x32,0x52,0x9e,0x77,0xbb,0xdb,0x17,0x0e,0xc2,0xa2,0x6e,
      0x7a,0xb6,0xd6,0x1a,0x03,0xcf,0xaf,0x63,0x8a,0x46,0x26,0xea,0xf3,0x3f,0x5f,0x93,
      0xa9,0x65,0x05,0xc9,0xd0,0x1c,0x7c,0xb0,0x59,0x95,0xf5,0x39,0x20,0xec,0x8c,0x40,
      0x54,0x98,0xf8,0x34,0x2d,0xe1,0x81,0x4d,0xa4,0x68,0x08,0xc4,0xdd,0x11,0x71,0xbd,
    };

    public MobcRfTmPacketExtractor(IPortManager portManager)
      : base(portManager, new ReceivedDataConfig
      {
        HeaderLength = 6,
        BodyLength = 434,
        FooterLength = 4
      })
    {
    }

    protected override void NewDataHandle(object sender, DataEventArgs e)
    {
      _buffer.AddRange(e.Data);
      while (_buffer.Count >= _config.HeaderLength)
      {
        if (AnalyzeHeader())
        {
          if (_buffer.Count >= _config.TotalLength + ReedSolomonCodeLength)
          {
            var receivedDataInbyteArrayWithReedSolomonCodes = _buffer.GetRange(0, _config.TotalLength + ReedSolomonCodeLength).ToArray();
            ReedSolomonDecode(ref receivedDataInbyteArrayWithReedSolomonCodes);

            if (AnalyzeFooter())
            {
              var receivedDataInByteArray = receivedDataInbyteArrayWithReedSolomonCodes.ToList().GetRange(0, _config.TotalLength).ToArray();
              var data = ConvertToTmPacketData(receivedDataInByteArray);
              _packetQueue.Enqueue(data);
              _buffer.RemoveRange(0, _config.TotalLength + ReedSolomonCodeLength);
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
      var ETX = _buffer.GetRange(_config.TotalLength - _config.FooterLength, _config.FooterLength).ToArray();
      //only use fixed bits
      if (!ChkCtlWrdType(ETX, CtlWrdType.CLCW)) { return false; }
      if (!ChkClcwVer(ETX, ClcwVer.Ver1)) { return false; }
      if (!ChkCOPinEff(ETX, COPinEff.COP1)) { return false; }
      if (!ChkVCId(ETX, VCId.Default)) { return false; }
      if (!ChkSpare(ETX, Spare.Fixed)) { return false; }
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
    
    protected override void RemoveAllData()
    {
      _buffer.Clear();
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

    private void ReedSolomonDecode(ref byte[] byteArrayWithReedSolomonCode)  // specific to interleave2(shortened)
    {
      // Add zero fill to head
      byte[] zerofill = { 0, 0 };
      byte[] byteArrayWithReedSolomonCodeZeroFilled = zerofill.Concat(byteArrayWithReedSolomonCode).ToArray();

      // divide to 2 byte array
      byte[] byteArray1 = new byte[255];
      byte[] byteArray2 = new byte[255];
      for (int i = 0; i < 510; ++i)
      {
        int index = i / 2;
        if (i % 2 == 0)  // even
        {
          byteArray1[index] = byteArrayWithReedSolomonCodeZeroFilled[i];
        }
        else  // odd
        {
          byteArray2[index] = byteArrayWithReedSolomonCodeZeroFilled[i];
        }
      }

      // decode each byte array block
      ReedSolomonDecodeOneBlock(ref byteArray1);
      ReedSolomonDecodeOneBlock(ref byteArray2);

      // joint 2 byte array
      for (int i = 2; i < 510 - ReedSolomonCodeLength; ++i)
      {
        int index = i / 2;
        if (i % 2 == 0)  // even
        {
          byteArrayWithReedSolomonCodeZeroFilled[i] = byteArray1[index];
        }
        else  // odd
        {
          byteArrayWithReedSolomonCodeZeroFilled[i] = byteArray2[index];
        }
      }

      // Remove zero fill
      byteArrayWithReedSolomonCode = byteArrayWithReedSolomonCodeZeroFilled.ToList().GetRange(2, 508).ToArray();
    }
    private void ReedSolomonDecodeOneBlock(ref byte[] byteArrayWithReedSolomonCode)
    {
      int[] erasures_pos = new int[NROOTS];
      int erasures_num = 0;
      byte[] cdata = new byte[NN];

      // Convert data from dual basis to conventional
      for (int i = 0; i < NN; ++i)
        cdata[i] = Tal1tab[byteArrayWithReedSolomonCode[i]];

      int r = ReedSolomonErrorCheckAndCorrect(cdata, erasures_pos, erasures_num);

      if (r > 0)
      {
        // Convert from conventional to dual basis
        for (int i = 0; i < NN; ++i)
          byteArrayWithReedSolomonCode[i] = Taltab[cdata[i]];
      }
      else
      {
        // Console.Write("No errors detected in RS codes");
      }
    }
    private int ReedSolomonErrorCheckAndCorrect(byte[] data, int[] erasures_pos, int erasures_num)
    {
      int[] lambda = new int[NROOTS + 1];  // Err+Eras Locator poly
      int[] s = new int[NROOTS]; // syndrome poly
      int[] b = new int[NROOTS + 1];
      int[] t = new int[NROOTS + 1];
      int[] omega = new int[NROOTS + 1];
      int[] root = new int[NROOTS];
      int[] reg = new int[NROOTS + 1];
      int[] loc = new int[NROOTS];
      int count;

      // form the syndromes; i.e., evaluate data(x) at roots of g(x)
      for (int i = 0; i < NROOTS; ++i)
        s[i] = data[0];

      for (int j = 1; j < NN; ++j)
      {
        for (int i = 0; i < NROOTS; ++i)
        {
          if (s[i] == 0)
          {
            s[i] = data[j];
          }
          else
          {
            s[i] = data[j] ^ CCSDS_alpha_to[mod255(CCSDS_index_of[s[i]] + (FCR + i) * PRIM)];
          }
        }
      }

      // Convert syndromes to index form, checking for nonzero condition
      int syn_error = 0;
      for (int i = 0; i < NROOTS; ++i)
      {
        syn_error |= s[i];
        s[i] = CCSDS_index_of[s[i]];
      }

      if (syn_error == 0)
      {
        // if syndrome is zero, data[] is a codeword and there are no errors to correct. So return data[] unmodified
        count = 0;
        goto finish;
      }
      lambda[0] = 1;

      if (erasures_num > 0)
      {
        int u = 0;
        int u_tmp = 0;
        // Init lambda to be the erasure locator polynomial
        lambda[1] = CCSDS_alpha_to[mod255(PRIM * (NN - 1 - erasures_pos[0]))];
        for (int i = 1; i < erasures_num; ++i)
        {
          u = mod255(PRIM * (NN - 1 - erasures_pos[i]));
          for (int j = i + 1; j > 0; --j)
          {
            u_tmp = CCSDS_index_of[lambda[j - 1]];
            if (u_tmp != A0)
              lambda[j] ^= CCSDS_alpha_to[mod255(u + u_tmp)];
          }
        }
      }
      for (int i = 0; i < NROOTS + 1; ++i)
        b[i] = CCSDS_index_of[lambda[i]];

      // Begin Berlekamp-Massey algorithm to determine error+erasure locator polynomial
      int r = erasures_num;
      int el = erasures_num;
      int discr_r = 0;
      while (++r <= NROOTS)  // r is the step number
      {
        // Compute discrepancy at the r-th step in poly-form
        discr_r = 0;
        for (int i = 0; i < r; ++i)
        {
          if ((lambda[i] != 0) && (s[r - i - 1] != A0))
          {
            discr_r ^= CCSDS_alpha_to[mod255(CCSDS_index_of[lambda[i]] + s[r - i - 1])];
          }
        }
        discr_r = CCSDS_index_of[discr_r];  // Index form
        if (discr_r == A0)
        {
          // 2 lines below: B(x) <-- x*B(x)
          var b_list = new List<int>(b);
          b_list.Insert(0, A0);
          b_list.RemoveAt(NROOTS + 1);
          b = b_list.ToArray();
        }
        else
        {
          // 7 lines below: T(x) <-- lambda(x) - discr_r*x*b(x)
          t[0] = lambda[0];
          for (int i = 0; i < NROOTS; ++i)
          {
            if (b[i] != A0)
              t[i + 1] = lambda[i + 1] ^ CCSDS_alpha_to[mod255(discr_r + b[i])];
            else
              t[i + 1] = lambda[i + 1];
          }
          if (2 * el <= r + erasures_num - 1)
          {
            el = r + erasures_num - el;
            // 2 lines below: B(x) <-- inv(discr_r) * lambda(x)
            for (int i = 0; i <= NROOTS; ++i)
              b[i] = (lambda[i] == 0) ? A0 : mod255(CCSDS_index_of[lambda[i]] - discr_r + NN);
          }
          else
          {
            // 2 lines below: B(x) <-- x*B(x)
            var b_list = new List<int>(b);
            b_list.Insert(0, A0);
            b_list.RemoveAt(NROOTS + 1);
            b = b_list.ToArray();
          }
          Array.Copy(t, lambda, NROOTS + 1);
        }
      }

      // Convert lambda to index form and compute deg(lambda(x))
      int deg_lambda = 0;
      for (int i = 0; i < NROOTS + 1; ++i)
      {
        lambda[i] = CCSDS_index_of[lambda[i]];
        if (lambda[i] != A0)
          deg_lambda = i;
      }
      // Find roots of the error+erasure locator polynomial by Chien search
      Array.Copy(lambda, reg, NROOTS);
      count = 0;  // Number of roots of lambda(x)
      int q = 0;
      for (int i = 1, k = IPRIM - 1; i <= NN; ++i, k = mod255(k + IPRIM))
      {
        q = 1;  // lambda[0] is always 0
        for (int j = deg_lambda; j > 0; --j)
        {
          if (reg[j] != A0)
          {
            reg[j] = mod255(reg[j] + j);
            q ^= CCSDS_alpha_to[reg[j]];
          }
        }
        if (q != 0)
          continue;  // Not a root
        // store root (index-form) and error location number
        root[count] = i;
        loc[count] = k;
        // If we've already found max possible roots, abort the search to save time
        if (++count == deg_lambda)
          break;
      }
      if (deg_lambda != count)
      {
        // deg(lambda) unequal to number of roots => uncorrectable error detected
        Console.Write("deg(lambda) unequal to number of roots => uncorrectable error detected (deg_lambda={0} != count={1})\n", deg_lambda, count);
        count = -1;
        goto finish;
      }
      
      // Compute err+eras evaluator poly omega(x) = s(x)*lambda(x) (modulo x**NROOTS). in index form. Also find deg(omega).
      int deg_omega = 0;
      int omega_tmp = 0;
      for (int i = 0; i < NROOTS; ++i)
      {
        omega_tmp = 0;
        int j = (deg_lambda < i) ? deg_lambda : i;
        for (; j >= 0; --j)
        {
          if ((s[i - j] != A0) && (lambda[j] != A0))
            omega_tmp ^= CCSDS_alpha_to[mod255(s[i - j] + lambda[j])];
        }
        if (omega_tmp != 0)
          deg_omega = i;
        omega[i] = CCSDS_index_of[omega_tmp];
      }
      omega[NROOTS] = A0;

      // Compute error values in poly-form. num1 = omega(inv(X(l))), num2 = inv(X(l))**(FCR-1) and den = lambda_pr(inv(X(l))) all in poly-form
      int num1 = 0;
      int num2 = 0;
      int den = 0;
      for (int j = count - 1; j >= 0; --j)
      {
        num1 = 0;
        for (int i = deg_omega; i >= 0; --i)
        {
          if (omega[i] != A0)
            num1 ^= CCSDS_alpha_to[mod255(omega[i] + i * root[j])];
        }
        num2 = CCSDS_alpha_to[mod255(root[j] * (FCR - 1) + NN)];
        den = 0;

        // lambda[i+1] for i even is the formal derivative lambda_pr of lambda[i]
        for (int i = Math.Min(deg_lambda, NROOTS - 1) & ~1; i >= 0; i -= 2)
        {
          if (lambda[i + 1] != A0)
            den ^= CCSDS_alpha_to[mod255(lambda[i + 1] + i * root[j])];
        }
        if (den == 0)
        {
          count = -1;
          goto finish;
        }
        // Apply error to data
        if (num1 != 0)
        {
          data[loc[j]] ^= CCSDS_alpha_to[mod255(CCSDS_index_of[num1] + CCSDS_index_of[num2] + NN - CCSDS_index_of[den])];
        }
      }
    finish:
      if (erasures_pos != null)
      {
        for (int i = 0; i < count; ++i)
            erasures_pos[i] = loc[i];
      }
      return count;
    }
    
    private int mod255(int x)
    {
      while (x >= 255)
      {
        x -= 255;
        x = (x >> 8) + (x & 255);
      }
      return x;
    }
  }
}
