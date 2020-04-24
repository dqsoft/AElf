using System;
using System.Net;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Protocol;
using AElf.OS.Network.Protocol.Types;
using AElf.OS.Network.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule), typeof(GrpcNetworkModule))]
    public class ConnectionServiceTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<NetworkOptions>(o => { o.MaxPeersPerIpAddress = 1; });

            context.Services.AddSingleton(sp =>
            {
                Mock<IPeerDialer> mockDialer = new Mock<IPeerDialer>();
                
                mockDialer.Setup(d => d.DialBackPeerAsync(It.IsAny<DnsEndPoint>(), It.IsAny<Handshake>()))
                    .Returns<DnsEndPoint, Handshake>((ip, hsk) =>
                    {
                        return Task.FromResult(new GrpcPeer(
                                new GrpcClient(null, Mock.Of<PeerService.PeerServiceClient>()), ip, new PeerConnectionInfo
                                {
                                    Pubkey = hsk.HandshakeData.Pubkey.ToHex(),
                                    ConnectionTime = TimestampHelper.GetUtcNow()
                                }));
                    });
                
                mockDialer.Setup(d => d.DialBackPeerAsync(It.Is<DnsEndPoint>(ipEndpoint => ipEndpoint.Host.Equals("1.2.3.5")), It.IsAny<Handshake>()))
                    .Returns<DnsEndPoint, Handshake>(async (ip, hsk) =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        
                        return new GrpcPeer(
                            new GrpcClient(null, Mock.Of<PeerService.PeerServiceClient>()), ip, new PeerConnectionInfo
                            {
                                Pubkey = hsk.HandshakeData.Pubkey.ToHex(),
                                ConnectionTime = TimestampHelper.GetUtcNow()
                            });
                    });

                return mockDialer.Object;
            });

            context.Services.AddSingleton<IHandshakeProvider>(sp =>
            {
                Mock<IHandshakeProvider> mockHskPro = new Mock<IHandshakeProvider>();
                mockHskPro.Setup(p => p.GetHandshakeAsync())
                    .Returns(Task.FromResult(new Handshake {SessionId = ByteString.CopyFrom(new byte[] {0, 1, 2}), HandshakeData = new HandshakeData()}));
                mockHskPro.Setup(p => p.ValidateHandshakeAsync(It.IsAny<Handshake>()))
                    .Returns(Task.FromResult(HandshakeValidationResult.Ok));
                return mockHskPro.Object;
            });
        }
    }
}