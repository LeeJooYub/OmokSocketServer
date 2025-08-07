using System;

namespace SocketServer;

class Program
{
    // 실행할 때는 아래 명령어로 실행한다.
    //dotnet run -- --uniqueID 1 --name OmokServer --maxConnectionNumber 100 --port 5000 --maxRequestLength 4096 --roomMaxCount 16 --roomMaxUserCount 2 --roomStartNumber 1
    static void Main(string[] args)
    {
        Console.WriteLine("Hello SuperSocketLite");

        // 명령어 인수로 서버 옵셥을 받아서 처리한다.
        var serverOption = ParseCommandLine(args);
        if (serverOption == null)
        {
            return;
        }



        // 서버를 생성하고 초기화한다.
        var server = new MainServer();
        server.InitConfig(serverOption);
        server.CreateServer();
        // 서버를 시작한다.
        var IsResult = server.Start();



        if (IsResult)
        {
            MainServer.s_MainLogger.Info("서버 네트워크 시작");
        }
        else
        {
            Console.WriteLine("서버 네트워크 시작 실패");
            return;
        }



        MainServer.s_MainLogger.Info("Press q to shut down the server");
        // q 키를 누르면 서버를 종료한다.
        while (true)
        {
            System.Threading.Thread.Sleep(50);
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.KeyChar == 'q')
                {
                    Console.WriteLine("Server Terminate ~~~");
                    server.StopServer();
                    break;
                }
            }
                            
        }
    }

    static ServerOption ParseCommandLine(string[] args)
    {
        var result = CommandLine.Parser.Default.ParseArguments<ServerOption>(args) as CommandLine.Parsed<ServerOption>;

        if (result == null)
        {
            System.Console.WriteLine("Failed Command Line");
            return null;
        }

        return result.Value;
    }              

}

