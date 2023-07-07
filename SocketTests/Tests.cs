using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketTests;

public class Tests
{
    public const int Port = 65432;

    private readonly Encoding _encoding = Encoding.ASCII;

    [Test]
    public async Task Test()
    {
        CancellationTokenSource clientCts = new CancellationTokenSource();

        var serverTask = RunServer();
        var clientTask = RunClient(clientCts.Token);

        await serverTask;

        clientCts.Cancel();
        await clientTask;
    }

    private async Task RunServer()
    {
        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Loopback, Port);

        using Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        socket.Bind(ipEndPoint);

        socket.Listen(100);

        using Socket acceptedSocket = await socket.AcceptAsync();

        byte[] bufferToReceive = new byte[1024];
        int bytesReceived = await acceptedSocket.ReceiveAsync(bufferToReceive, SocketFlags.None);
        string responseText = _encoding.GetString(bufferToReceive, 0, bytesReceived);
        TestContext.WriteLine("Server got: " + responseText);

        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(500);
            byte[] bufferToSend = _encoding.GetBytes($"Hello, client ({i})!");
            await acceptedSocket.SendAsync(bufferToSend, SocketFlags.None);
        }
    }

    private async Task RunClient(CancellationToken cancellationToken = default)
    {
        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Loopback, Port);

        using Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        await socket.ConnectAsync(ipEndPoint);

        byte[] bufferToSend = _encoding.GetBytes("Hello, server!");
        await socket.SendAsync(bufferToSend, SocketFlags.None);

        while (!cancellationToken.IsCancellationRequested)
        {
            byte[] bufferToReceive = new byte[1024];
            int bytesReceived = await socket.ReceiveAsync(bufferToReceive, SocketFlags.None);

            if (bytesReceived > 0)
            {
                string responseText = _encoding.GetString(bufferToReceive, 0, bytesReceived);
                TestContext.WriteLine($"Client got: " + responseText);
            }
        }
    }
}
