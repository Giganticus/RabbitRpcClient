namespace RabbitRpcClient;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("RPC Client");
        
        await InvokeAsync("{\"rpcField\":\"rpcValue\"}");

        Console.WriteLine("Press [enter] to exit.");
        Console.ReadLine();
    }

    private static async Task InvokeAsync(string n)
    {
        using var rpcClient = new RpcClient();

       // Console.WriteLine(" [x] Requesting fib({0})", n);
        var response = await rpcClient.CallAsync(n);
        Console.WriteLine("Got '{0}'", response);
    }
}