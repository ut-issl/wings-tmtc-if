using System;

namespace WINGS_TMTC_IF.Models
{
  public class DataEventArgs : EventArgs
  {
    public byte[] Data;
    
    public DataEventArgs(byte[] dataInByteArray)
    {
      Data = dataInByteArray;
    }
  }
}