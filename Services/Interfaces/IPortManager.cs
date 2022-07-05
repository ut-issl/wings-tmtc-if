using System;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services
{
  public interface IPortManager
  {
    void Initialize();
    event EventHandler<DataEventArgs> NewDataReceived;
    void Write(byte[] data, int offset, int count);
    string ConsoleSelectValue(string name, string[] options)
    {
      int num;
      Console.WriteLine("Select {0} and press enter", name);
      for (int i = 0; i < options.Length; i++)
      {
        Console.WriteLine("[ {0} ] : {1}", i, options[i]);
      }
      while (true)
      {
        try
        {
          num = int.Parse(Console.ReadLine());
          if (num < options.Length)
          {
            return options[num];
          }
        }
        catch
        {
        }
        Console.WriteLine("Error unexpeced input");
      }
    }
  }
}
