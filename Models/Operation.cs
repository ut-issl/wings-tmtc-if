using System;

namespace WINGS_TMTC_IF.Models
{
  public class Operation
  {
    public string Id { get; set; }
    public string PathNumber { get; set; }
    public string Comment { get; set; }
    public string CommanderId { get; set; }
    public bool IsTmtcConnected { get; set; }
  }
}
