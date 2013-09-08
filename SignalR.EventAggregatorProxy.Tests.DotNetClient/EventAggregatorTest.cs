﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using SignalR.EventAggregatorProxy.Client.Bootstrap.Factories;
using SignalR.EventAggregatorProxy.Client.EventAggregation;

namespace SignalR.EventAggregatorProxy.Tests.DotNetClient
{
    public abstract class Event
    {
        
    }

    public class StandardEvent : Event
    {
        
    }

    [TestClass]
    public class When_subscribing_to_multiple_events_of_same_type : DotNetClientTest
    {
        private int subscriptionCount = 0;

        [TestInitialize]
        public void Context()
        {
            var reset = new AutoResetEvent(false);

            var proxy = Mock<IHubProxy>();
            WhenCalling<IHubProxy>(x => x.Subscribe(Arg<string>.Is.Anything))
                .Return(new Subscription());
            WhenCalling<IHubProxy>(x => x.Invoke(Arg<string>.Is.Equal("subscribe"), Arg<object[]>.Is.Anything))
                .Callback<string, object[]>((m, a) =>
                    {
                        subscriptionCount += (a[0] as IEnumerable<dynamic>).Count();
                        reset.Set();
                        return true;
                    })
                .Return(null);

            Mock<IHubProxyFactory>();
            
            WhenCalling<IHubProxyFactory>(x => x.Create(Arg<string>.Is.Anything, Arg<Action<IHubConnection>>.Is.Anything, Arg<Action<IHubProxy>>.Is.Anything))
                .Callback<string, Action<IHubConnection>, Action<IHubProxy>>((u, c, started) =>
                    {
                        started(proxy);
                        return true;
                    })
                .Return(proxy);

            var eventAggregator = new EventAggregator<Event>()
                .Init("foo");

            for (int i = 0; i < 2; i++)
                eventAggregator.Subscribe(Mock<IHandle<StandardEvent>>());

            reset.WaitOne(50);
        }

        [TestMethod]
        public void It_should_only_call_server_side_subscribe_once()
        {
            Assert.AreEqual(1, subscriptionCount);
        }
    }
}
