namespace WINGS_TMTC_IF.Models
{
  public class ReceivedDataConfig
  {
    public int HeaderLength { get; set; }
    public int BodyLength { get; set; }
    public int FooterLength { get; set; }
    public int TotalLength { get { return HeaderLength + BodyLength + FooterLength; } }
  }
}
