using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
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
            routingHandler.AddHandler("http://foo",  fooHandler);
            routingHandler.AddHandler("http://bar",  barHandler);
            var client = new HttpClient(routingHandler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://bar");
            
            var response = await client.SendAsync(request);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            fooHandler.Last.ShouldBeNull();
            barHandler.Last.ShouldBe(request);
        }

        [Fact]
        public void When_add_duplicate_origins_then_should_throw()
        {
            var routingHandler = new RoutingMessageHandler();
            routingHandler.AddHandler("http://foo", new Handler());
            
            Action act = () => routingHandler.AddHandler("http://foo", new Handler());

            act.ShouldThrow<ArgumentException>();
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

        [Fact]
        public async Task Example_with_TestServer()
        {
            var routingHandler = new RoutingMessageHandler();

            // Create the two Test Servers
            var fooWebHostBuilder = WebHost.CreateDefaultBuilder<FooStartup>(Array.Empty<string>());
            var fooTestServer = new TestServer(fooWebHostBuilder);

            var barWebHostBuilder = WebHost.CreateDefaultBuilder<BarStartup>(Array.Empty<string>());
            var barTestServer = new TestServer(barWebHostBuilder);

            // Register the test servers against their respective origins
            routingHandler.AddHandler("http://foo", fooTestServer.CreateHandler());
            routingHandler.AddHandler("http://bar", barTestServer.CreateHandler());

            // Configure your HttpClient with the routing handler
            var client = new HttpClient(routingHandler);

            // Requests to specific origins should be routed to the correct TestServer
            var fooResponse = await client.GetAsync("http://foo");
            (await fooResponse.Content.ReadAsStringAsync()).ShouldBe("foo");

            var barResponse = await client.GetAsync("http://bar");
            (await barResponse.Content.ReadAsStringAsync()).ShouldBe("bar");
        }

        [Fact]
        public async Task If_host_not_found_then_should_throw()
        {
            var routingHandler = new RoutingMessageHandler();
            routingHandler.AddHandler("localhost", 80, new HttpClientHandler());
            routingHandler.AddHandler("example.com", 80, new HttpClientHandler());

            var httpClient = new HttpClient(routingHandler);

            Exception exception = null;
            try
            {
                await httpClient.GetAsync("http://test");
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            exception.ShouldBeOfType<InvalidOperationException>();
        }

        [Fact]
        public async Task Execution_context_flow_should_no_flow_to_handler()
        {
            var routingHandler = new RoutingMessageHandler();
            var asyncLocal = new AsyncLocal<Guid>
            {
                Value = Guid.NewGuid()
            };
            var outside = asyncLocal.Value;
            var inside = outside;

            var handler = new DelegateHandler(_ =>
            {
                inside = asyncLocal.Value;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });
            routingHandler.AddHandler("localhost", 80, handler);
            var httpClient = new HttpClient(routingHandler);
            
            await httpClient.GetAsync("http://localhost");

            inside.ShouldNotBe(outside);
        }

        public class FooStartup
        {
            public void Configure(IApplicationBuilder app)
            {
                app.Run(ctx => ctx.Response.WriteAsync("foo"));
            }
        }

        public class BarStartup
        {
            public void Configure(IApplicationBuilder app)
            {
                app.Run(ctx => ctx.Response.WriteAsync("bar"));
            }
        }

        private class DelegateHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

            public DelegateHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _handler(request);
            }
        }
    }
}
