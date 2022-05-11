using Google.Protobuf;
using Grpc.Net.Client;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcTestClient
{
    internal class Program
    {
        private const int RequestDataSize = 290699;
        private const int ResponseDataSize = 24512;

        private static async Task Main(string[] args)
        {
            using (var channel = GrpcChannel.ForAddress("http://localhost:81", new GrpcChannelOptions
            {
                //HttpHandler = new WinHttpHandler()
            }))
            {
                var client = new TestProxy.TestProxy.TestProxyClient(channel);
                var request = new TestProxy.ForwardRequest
                {
                    Data = ByteString.CopyFrom(new byte[RequestDataSize]),
                    ResponseDataSize = ResponseDataSize
                };

                // warm up like establishing connection
                while (true)
                {
                    request.TraceId = Guid.NewGuid().ToString("N");
                    var ret = await SendRequestAsync(client, request, true);
                    if (ret > 0)
                    {
                        break;
                    }

                    Thread.Sleep(1000);
                }

                Console.WriteLine("Ready to send simulated requests");

                var latenciesInUs = new long[10000];
                Array.Clear(latenciesInUs);
                int startIndex = 0;
                int totalCount = 0;
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    totalCount++;
                    request.TraceId = Guid.NewGuid().ToString("N");
                    var latencyInUs = await SendRequestAsync(client, request, false);
                    if (latencyInUs > 0)
                    {
                        latenciesInUs[startIndex++] = latencyInUs;
                    }

                    if (startIndex == latenciesInUs.Length)
                    {
                        Array.Sort(latenciesInUs);
                        DateTime endTime = DateTime.Now;
                        var qps = totalCount * 1.0 / (endTime - startTime).TotalSeconds;
                        var successRate = latenciesInUs.Length * 1.0 / totalCount;

                        Console.WriteLine($"successful rate {successRate} = {latenciesInUs.Length}/{totalCount}");
                        Console.WriteLine($"qps {qps} = {totalCount}/({endTime.ToString("yyyy-MM-dd HH:mm:ss")} - {startTime.ToString("yyyy-MM-dd HH:mm:ss")})");
                        Console.WriteLine($"min   latency in Us: {latenciesInUs.First()}");
                        Console.WriteLine($"max   latency in Us: {latenciesInUs.Last()}");
                        Console.WriteLine($"50%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.5)]}");
                        Console.WriteLine($"90%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.9)]}");
                        Console.WriteLine($"95%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.95)]}");
                        Console.WriteLine($"99%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.99)]}");
                        Console.WriteLine($"99.9% latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.999)]}");

                        startIndex = 0;
                        totalCount = 0;
                        startTime = DateTime.Now;
                        Array.Clear(latenciesInUs);
                    }
                }
            }
        }

        private static async Task<long> SendRequestAsync(TestProxy.TestProxy.TestProxyClient client, TestProxy.ForwardRequest request, bool logResponse)
        {
            long latencyInUs = -1;
            try
            {
                var gen0Before = GC.CollectionCount(0);
                var gen1Before = GC.CollectionCount(1);
                var gen2Before = GC.CollectionCount(2);
                var watch = StopwatchWrapper.StartNew();
                watch.Start();
                var response = await client.ForwardAsync(request).ResponseAsync;
                watch.Stop();

                if (logResponse)
                {
                    Console.WriteLine($"Response is {JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true })}");
                }

                var gapLatencyInUs = watch.ElapsedInUs - response.RouteLatencyInUs;

                if (gapLatencyInUs > 30000)
                {
                    var gen0After = GC.CollectionCount(0);
                    var gen1After = GC.CollectionCount(1);
                    var gen2After = GC.CollectionCount(2);

#pragma warning disable SA1118 // Parameter should not span multiple lines
                    Console.WriteLine(
                        $"It takes {gapLatencyInUs}us ({((double)gapLatencyInUs / 1000000).ToString("0.####")}sec) to send {request.Data.Length} bytes on network by trace id {request.TraceId}. " +
                        $"ClientSendingTime={watch.StartTime.ToString("HH:mm:ss:fff")}, RouteStartTimeOnServer={new DateTime(response.RouteStartTimeInTicks).ToString("HH:mm:ss:fff")}, " +
                        $"RouteLatencyOnServerInUs={response.RouteLatencyInUs}, RouteClientLatencyInUs={watch.ElapsedInUs}. " +
                        $"Gen0/1/2 Before {gen0Before}/{gen1Before}/{gen2Before} After {gen0After}/{gen1After}/{gen2After}.");
#pragma warning restore SA1118 // Parameter should not span multiple lines
                }

                latencyInUs = watch.ElapsedInUs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"failed to send a request. Exception : {ex}");
            }

            return latencyInUs;
        }
    }
}
