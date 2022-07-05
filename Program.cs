using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WINGS_TMTC_IF.Services;
using WINGS_TMTC_IF.Services.MOBC;
using WINGS_TMTC_IF.Services.IsslCommon;

namespace WINGS_TMTC_IF
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var services = ConfigureServices();

      var serviceProvider = services.BuildServiceProvider();
      
      serviceProvider.GetService<App>().Run();
    }

    private static IServiceCollection ConfigureServices()
    {
      IServiceCollection services = new ServiceCollection();

      var configuration = LoadConfiguration();
      services.AddSingleton(configuration);

      services.AddTransient<App>();
      services.AddTransient<IOperationService, OperationService>();
      services.AddTransient<TmtcPacketService>();

      ConfigureUserDefinedServices(services, configuration);
      
      return services;
    }

    private static IConfiguration LoadConfiguration()
    {
      var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

      return builder.Build();
    }

    private static void ConfigureUserDefinedServices(IServiceCollection services, IConfiguration configuration)
    {
      var component = ConsoleSelectComponent(configuration);
      
      switch (component)
      {
        case "MOBC_UART":
          services.AddSingleton<IPortManager, SerialPortManager>();
          services.AddSingleton<ITmPacketExtractor, MobcUartTmPacketExtractor>();
          services.AddSingleton<ITcPacketHandler, MobcUartTcPacketHandler>();
          configuration["SerialPort:BaudRate:Using"] = configuration["SerialPort:BaudRate:MOBC_UART"];
          break;
        
        case "MOBC_RF":
          services.AddSingleton<IPortManager, LanPortManager>();
          services.AddSingleton<ITmPacketExtractor, MobcRfTmPacketExtractor>();
          services.AddSingleton<ITcPacketHandler, MobcRfTcPacketHandler>();
          configuration["SerialPort:BaudRate:Using"] = configuration["SerialPort:BaudRate:MOBC_RF"];
          break;

        case "ISSL_COMMON":
          services.AddSingleton<IPortManager, SerialPortManager>();
          services.AddSingleton<ITmPacketExtractor, IsslCommonTmPacketExtractor>();
          services.AddSingleton<ITcPacketHandler, IsslCommonTcPacketHandler>();
          configuration["SerialPort:BaudRate:Using"] = configuration["SerialPort:BaudRate:MIF"];
          break;

        default:
          throw new Exception();
      }
    }

    private static string ConsoleSelectComponent(IConfiguration configuration)
    {
      int num;
      var compos = configuration.GetSection("ComponentList").Get<string[]>();

      Console.WriteLine("Select component and press enter");
      for (int i = 0; i < compos.Length; i++)
      {
        Console.WriteLine("[ {0} ] : {1}", i, compos[i]);
      }
      while (true)
      {
        try
        {
          num = int.Parse(Console.ReadLine());
          if (num < compos.Length)
          {
            return compos[num];
          }
        }
        catch
        {
        }
        Console.WriteLine("Error unexpected input");
      }
    }
  }
}
