﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Extensions;
using GitHub.Services;
using NSubstitute;
using Rothko;
using Xunit;

namespace UnitTests.GitHub.App.Services
{
    public class OAuthCallbackListenerTests
    {
        [Fact]
        public void ListenStartsHttpListener()
        {
            var httpListener = CreateHttpListener("id1");
            var target = new OAuthCallbackListener(httpListener);

            target.Listen("id1", CancellationToken.None).Forget();

            httpListener.Prefixes.Received(1).Add("http://localhost:42549/");
            httpListener.Received(1).Start();
        }

        [Fact]
        public async Task ListenStopsHttpListener()
        {
            var httpListener = CreateHttpListener("id1");
            var target = new OAuthCallbackListener(httpListener);

            await target.Listen("id1", CancellationToken.None);

            httpListener.Received(1).Stop();
        }

        [Fact]
        public void CancelStopsHttpListener()
        {
            var httpListener = CreateHttpListener(null);
            var cts = new CancellationTokenSource();
            var target = new OAuthCallbackListener(httpListener);

            var task = target.Listen("id1", cts.Token);
            httpListener.Received(0).Stop();

            cts.Cancel();
            httpListener.Received(1).Stop();
        }

        [Fact]
        public void CallingListenWhenAlreadyListeningCancelsFirstListen()
        {
            var httpListener = CreateHttpListener(null);

            var target = new OAuthCallbackListener(httpListener);
            var task1 = target.Listen("id1", CancellationToken.None);
            var task2 = target.Listen("id2", CancellationToken.None);

            httpListener.Received(1).Stop();
        }

        [Fact]
        public async Task SuccessfulResponseClosesResponse()
        {
            var httpListener = CreateHttpListener("id1");
            var context = httpListener.GetContext();
            var target = new OAuthCallbackListener(httpListener);

            await target.Listen("id1", CancellationToken.None);

            context.Response.Received(1).Close();
        }

        IHttpListener CreateHttpListener(string id)
        {
            var result = Substitute.For<IHttpListener>();
            result.When(x => x.Start()).Do(_ => result.IsListening.Returns(true));

            if (id != null)
            {
                var context = Substitute.For<IHttpListenerContext>();
                context.Request.Url.Returns(new Uri($"https://localhost:42549?code=1234&state={id}"));
                result.GetContext().Returns(context);
                result.GetContextAsync().Returns(context);
            }
            else
            {
                var tcs = new TaskCompletionSource<IHttpListenerContext>();
                result.GetContextAsync().Returns(tcs.Task);
            }

            return result;
        }
    }
}