using System;
using System.Linq;
using System.IO.Ports;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using WINGS_TMTC_IF.Models;

namespace WINGS_TMTC_IF.Services
{
  public class SerialPortManager : IPortManager
  {
    private SerialPort _serialPort;
    public event EventHandler<DataEventArgs> NewDataReceived;

    public SerialPortManager(IConfiguration configuration)
    {
      _serialPort = new SerialPort();
      _serialPort.BaudRate = Convert.ToInt32(configuration["SerialPort:BaudRate:Using"]);
      _serialPort.Parity = Parity.None;
      _serialPort.StopBits = StopBits.One;
      _serialPort.DataBits = 8;
      _serialPort.Handshake = Handshake.None;
      _serialPort.DataReceived += new SerialDataReceivedEventHandler(OnDataReceived);
    }

    ~SerialPortManager()
    {
      Dispose(false);
    }

    public void Initialize()
    {
      var portNames = SerialPort.GetPortNames()
              .Where(name => name.Contains("COM") || name.Contains("tty.usb"))
              .ToArray();
      var portName = ((IPortManager)this).ConsoleSelectValue("serial port", portNames);
      Console.WriteLine("Open {0}...", portName);
      Open(portName);
    }

    private void Open(string portName)
    {
      if (_serialPort.IsOpen)
      {
        _serialPort.Close();
      }

      _serialPort.PortName = portName;
      try
      {
        _serialPort.Open();
        Console.WriteLine("Success");
      }
      catch
      {
        Console.WriteLine("Error opening serial port " + portName);
        Environment.Exit(0);
      }
    }

    private void Close()
    {
      _serialPort.Close();
    }

    public void Write(byte[] data, int offset, int count)
    {
      if (_serialPort.IsOpen)
      {
        _serialPort.Write(data, offset, count);
      }
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
      if (_serialPort.IsOpen)
      {
        int dataLength = _serialPort.BytesToRead;
        byte[] data = new byte[dataLength];

        int numDataRead = _serialPort.Read(data, 0, dataLength);
        if (numDataRead == 0)
        {
          return;
        }

        if (NewDataReceived != null)
        {
          NewDataReceived(this, new DataEventArgs(data));
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
      if (disposing)
      {
        _serialPort.DataReceived -= new SerialDataReceivedEventHandler(OnDataReceived);
      }

      if (_serialPort.IsOpen)
      {
        _serialPort.Close();
      } 
      _serialPort.Dispose();
    }
  }
}
