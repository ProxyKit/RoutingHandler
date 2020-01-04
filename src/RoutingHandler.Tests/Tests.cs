using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace ProxyKit.RoutingHandler
{
    public class Tests
    {
        [Fact]
        public async Task Should_route_to_correct_handler()
        {
            var routingHandler = new RoutingMessageHandler();
            var fooHandler = new Handler();
            var barHandler = new Handler();
            routingHandler.AddHandler(new Origin("foo", 80),  fooHandler);
            routingHandler.AddHandler(new Origin("bar", 80),  barHandler);
            var client = new HttpClient(routingHandler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://bar");
            var response = await client.SendAsync(request);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            fooHandler.Last.ShouldBeNull();
            barHandler.Last.ShouldBe(request);
        }

        public class Handler : HttpMessageHandler
        {
            public HttpRequestMessage Last { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken _)
            {
                Last = request;
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                return Task.FromResult(response);
            }
        }
    }
}
