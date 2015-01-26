﻿using System;
using System.Reactive.Subjects;
using Hermes;
using Hermes.Flows;
using Hermes.Packets;
using Moq;
using Xunit;

namespace Tests
{
	public class ServerSpec
	{
		[Fact]
		public void when_server_does_not_start_then_connections_are_ignored ()
		{
			var sockets = new Subject<IChannel<byte[]>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>>();
			var factory = Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IChannel<byte[]>> ()) == packetChannel.Object);

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.Receiver)
				.Returns(packets);

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var server = new Server (sockets, factory, flowProvider, connectionProvider.Object, configuration);

			sockets.OnNext (Mock.Of<IChannel<byte[]>> (x => x.Receiver == new Subject<byte[]> ()));

			Assert.Equal (0, server.ActiveChannels);
		}

		[Fact]
		public void when_connection_established_then_active_connections_increases ()
		{
			var sockets = new Subject<IChannel<byte[]>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>>();
			var factory = Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IChannel<byte[]>> ()) == packetChannel.Object);

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.Receiver)
				.Returns(packets);

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var server = new Server (sockets, factory, flowProvider, connectionProvider.Object, configuration);

			server.Start ();

			sockets.OnNext (Mock.Of<IChannel<byte[]>> (x => x.Receiver == new Subject<byte[]> ()));

			Assert.Equal (1, server.ActiveChannels);
		}

		[Fact]
		public void when_server_closed_then_pending_connection_is_closed ()
		{
			var sockets = new Subject<IChannel<byte[]>> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.Receiver)
				.Returns(new Subject<IPacket> ());

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);
			var server = new Server (sockets, Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IChannel<byte[]>> ()) == packetChannel.Object), 
				flowProvider, connectionProvider.Object, configuration);

			server.Start ();

			var socket = new Mock<IChannel<byte[]>> ();

			sockets.OnNext (socket.Object);

			server.Stop ();

			packetChannel.Verify (x => x.Dispose ());
		}

		[Fact]
		public void when_receiver_error_then_closes_connection_and_decreases_connection_list ()
		{
			var sockets = new Subject<IChannel<byte[]>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();

			var packetChannel = new Mock<IChannel<IPacket>> ();
			var factory = new Mock<IPacketChannelFactory> ();

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.Receiver)
				.Returns(packets);

			factory.Setup (x => x.Create (It.IsAny<IChannel<byte[]>> ()))
				.Returns (packetChannel.Object);

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var server = new Server (sockets, factory.Object, flowProvider, connectionProvider.Object, configuration);
			var receiver = new Subject<byte[]> ();
			var socket = new Mock<IChannel<byte[]>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			server.Start ();

			sockets.OnNext (socket.Object);

			try {
				packets.OnError (new Exception ("Protocol exception"));
			} catch (Exception) {
			}

			packetChannel.Verify (x => x.Dispose ());
			Assert.Equal (0, server.ActiveChannels);
		}
	}
}
