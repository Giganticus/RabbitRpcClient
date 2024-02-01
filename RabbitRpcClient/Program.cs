namespace RabbitRpcClient;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("RPC Client Greeter using Rabbit libs directly");

        string? nameToGreet;
        
        using var rpcClient = new RpcClient();
        
        Console.WriteLine("Enter a name to receive a greeting. 'Quit' to quit.");
        while ((nameToGreet = Console.ReadLine()) != "Quit")
        {
            if(string.IsNullOrWhiteSpace(nameToGreet))
                continue;
            var response = await rpcClient.CallAsync(nameToGreet);
            Console.WriteLine($"Received: {response}");
        }
        
    }
}