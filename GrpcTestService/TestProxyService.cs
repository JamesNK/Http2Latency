using Google.Protobuf;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrpcTestService
{
    internal class TestProxyService : TestProxy.TestProxy.TestProxyBase
    {
        private static ByteString? ResponseData;

        public override Task<TestProxy.ForwardResponse> Forward(TestProxy.ForwardRequest request, ServerCallContext context)
        {
            var e2eWatch = StopwatchWrapper.StartNew();
            var response = new TestProxy.ForwardResponse();
            response.Data = GetResponseData(request);

            e2eWatch.Stop();
            response.RouteLatencyInUs = e2eWatch.ElapsedInUs;
            response.RouteStartTimeInTicks = e2eWatch.StartTime.Ticks;

            return Task.FromResult(response);
        }

        private static ByteString GetResponseData(TestProxy.ForwardRequest request)
        {
            // Avoid allocating response data every request.
            // Hacky but ByteString is immutable so this should work.
            var responseData = ResponseData;
            if (responseData?.Length != request.ResponseDataSize)
            {
                responseData = ResponseData = ByteString.CopyFrom(new byte[request.ResponseDataSize]);
            }
            return responseData;
        }
    }
}
