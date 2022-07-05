using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services
{
  public class LanPortManager : IPortManager
  {
    private bool _modEnable;
    private TcpClient _modTcpClient;
    private string _modIpAddress;
    private int _modPortNum;
    private bool _modIpAutoSearch;
    private NetworkStream _modStream;

    private bool _demodEnable;
    private UdpClient _demodUdpClient;
    private string _demodIpAddress;
    private int _demodPortNum;
    private bool _demodIpAutoSearch;
    private bool _demodConnected;
    public event EventHandler<DataEventArgs> NewDataReceived;

    public LanPortManager(IConfiguration configuration)
    {
      _modEnable = Convert.ToBoolean(configuration["LanPort:ModEnable"]);
      if (_modEnable)
      {
        _modTcpClient = new TcpClient();
        _modIpAddress = configuration["LanPort:ModIpAddress"].ToString();
        _modPortNum = Convert.ToInt32(configuration["LanPort:ModPortNum"]);
        _modIpAutoSearch = Convert.ToBoolean(configuration["LanPort:ModIpAutoSearch"]);
      }

      _demodEnable = Convert.ToBoolean(configuration["LanPort:DemodEnable"]);
      if (_demodEnable)
      {
        _demodUdpClient = new UdpClient();
        _demodIpAddress = configuration["LanPort:DemodIpAddress"].ToString();
        _demodPortNum = Convert.ToInt32(configuration["LanPort:DemodPortNum"]);
        _demodIpAutoSearch = Convert.ToBoolean(configuration["LanPort:DemodIpAutoSearch"]);
        _demodConnected = false;
      }
    }

    ~LanPortManager()
    {
      Dispose(false);
    }

    public void Initialize()
    {
      if (_modEnable)
      {
        ModInitialze();
      }
      if (_demodEnable)
      {
        DemodInitialze();
      }
    }

    private (string ipAddress, int portNum) GetIpAddressAndPortNum(string ipAddressDefault, int portNumDefault, bool ipAutoSearch)
    {
      string ipAddressResult = ipAddressDefault;
      int portNumResult = portNumDefault;

      Ping p = new Ping();
      var pingTimeout = 100; //[ms]

      if (ipAutoSearch)  // search valid ip address from all address in the same network & select from valid lists
      {
        const string ipPrefix = "192.168.0.";
        string ipTmp = string.Empty;
        List<string> validIpList = new List<string>();
        Console.WriteLine("Searching LAN port (about {0}sec) ... ", Math.Ceiling(Convert.ToDecimal(pingTimeout * 255 / 1000)));

        for (int i = 1; i <= 255; i++)
        {
          ipTmp = ipPrefix + i.ToString();

          PingReply pReply = p.Send(ipTmp, pingTimeout);
          if (pReply.Status == IPStatus.Success)
          {
            validIpList.Add(ipTmp);
          }
        }
        string[] validIps = validIpList.ToArray();
        
        ipAddressResult = ((IPortManager)this).ConsoleSelectValue("lan port", validIps);
        portNumResult = portNumDefault;
      }
      else  // use default ip address written in appsettings.json
      {
        PingReply pReply = p.Send(ipAddressDefault, pingTimeout);
        if (pReply.Status == IPStatus.Success)
        {
          Console.WriteLine("Ping check is successed");
          ipAddressResult = ipAddressDefault;
          portNumResult = portNumDefault;
        }
        else
        {
          Console.WriteLine("Error default LAN port don't reponse to ping");
          Environment.Exit(0);
        }
      }

      return (ipAddressResult, portNumResult);
    }

    private void ModInitialze()
    {
      string ipAddressDefault = _modIpAddress;
      int portNumDefault = _modPortNum;
      Console.WriteLine("Modulator default LAN port ... {0}:{1}", ipAddressDefault, portNumDefault);
      
      (_modIpAddress, _modPortNum) = GetIpAddressAndPortNum(ipAddressDefault, portNumDefault, _modIpAutoSearch);
      
      Console.WriteLine("Do you want to Open {0}:{1} as modulator ? [y/n]", _modIpAddress, _modPortNum);
      string ans;
      while (true)
      {
        try
        {
          ans = Console.ReadLine();
          if (ans == "y" || ans == "Y")
          {
            ModOpen(_modIpAddress, _modPortNum);
            return;
          }
          else if (ans == "n" || ans == "N")
          {
            Console.WriteLine("Please start program from first");
            Environment.Exit(0);
          }
        }
        catch
        {
        }
        Console.WriteLine("Error unexpected input");
      }
    }

    private void DemodInitialze()
    {
      string ipAddressDefault = _demodIpAddress;
      int portNumDefault = _demodPortNum;
      Console.WriteLine("Demodulator default LAN port ... {0}:{1}", ipAddressDefault, portNumDefault);
      
      (_demodIpAddress, _demodPortNum) = GetIpAddressAndPortNum(ipAddressDefault, portNumDefault, _demodIpAutoSearch);
      
      Console.WriteLine("Do you want to Open {0}:{1} as demodulator ? [y/n]", _demodIpAddress, _demodPortNum);
      string ans;
      while (true)
      {
        try
        {
          ans = Console.ReadLine() ;
          if (ans == "y" || ans == "Y")
          {
            DemodOpen(_demodIpAddress, _demodPortNum);
            return;
          }
          else if (ans == "n" || ans == "N")
          {
            Console.WriteLine("Please start program from first");
            Environment.Exit(0);
          }
        }
        catch
        {
        }
        Console.WriteLine("Error unexpected input");
      }
    }

    private void ModOpen(string ipAddress, int portNum)
    {
      if (_modTcpClient.Connected)
      {
        _modTcpClient.Close();
      }

      _modIpAddress = ipAddress;
      _modPortNum = portNum;
      try
      {
        _modTcpClient.Connect(_modIpAddress, _modPortNum);
        Console.WriteLine("Opening Modulator is Successed");
      }
      catch
      {
        Console.WriteLine("Error opening lan port " + _modIpAddress + ":" + _modPortNum);
        Environment.Exit(0);
      }
    }

    private void DemodOpen(string ipAddress, int portNum)
    {
      if (_demodConnected)
      {
        _demodUdpClient.Close();
      }

      _demodIpAddress = ipAddress;
      _demodPortNum = portNum;
      try
      {
        IPEndPoint localIpEndPoint = new IPEndPoint(IPAddress.Parse(_demodIpAddress), _demodPortNum);
        _demodUdpClient.Client.Bind(localIpEndPoint);

        _demodConnected = true;
        Console.WriteLine("Opening Demodulator is Successed");

        Task.Run(() => DataReceiveTask());
      }
      catch
      {
        Console.WriteLine("Error opening lan port " + _demodIpAddress + ":" + _demodPortNum);
        Environment.Exit(0);
      }
    }

    private void ModClose()
    {
      _modTcpClient.Close();
    }

    private void DemodClose()
    {
      _demodUdpClient.Close();
      _demodConnected = false;
    }

    public void Write(byte[] data, int offset, int count)
    {
      if (_modEnable && _modTcpClient.Connected)
      {
        try
        {
          _modStream = _modTcpClient.GetStream();
        }
        catch (System.Exception)
        {
          return;
        }

        _modStream.Write(data, offset, count);

        _modStream.Flush();
      }
    }

    private void DataReceiveTask()
    {
      while (_demodConnected)
      {
        IPEndPoint remoteIpEndPoint = null;
        try
        {
          byte[] data = _demodUdpClient.Receive(ref remoteIpEndPoint);

          if (NewDataReceived != null)
          {
            NewDataReceived(this, new DataEventArgs(data));
          }
        }
        catch (System.Exception)
        {
          return;
        }
      }
    }

    private void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (_modEnable)
      {
        if (_modTcpClient.Connected)
        {
          _modTcpClient.Close();
        } 
        _modTcpClient.Dispose();
      }

      if (_demodEnable)
      {
        if (_demodConnected)
        {
          _demodUdpClient.Close();
          _demodConnected = false;
        } 
        _demodUdpClient.Dispose();
      }
    }
  }
}
