using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.MixedReality.WebRTC;


namespace RCM_Coop.Network
{
    internal class WebRTC
    {

            // Replace with your TCP port
            private const int MyTcpPort = 50000;

            static async Task Main(string[] args)
            {
                Console.WriteLine("=== WebRTC + TCP Hybrid Peer ===");
                Console.WriteLine("Are you the OFFER side? (y/n)");
                bool isOfferSide = Console.ReadLine()?.Trim().ToLower() == "y";

                // Create peer connection
                using var pc = new PeerConnection();

                // Events for debugging
                pc.Connected += () => Console.WriteLine("PeerConnection: connected.");
                pc.IceStateChanged += (IceConnectionState newState) =>
                    Console.WriteLine($"ICE state: {newState}");
                pc.LocalSdpReadytoSend += message =>
                {
                    Console.WriteLine("\n=== LOCAL SDP ===");
                    Console.WriteLine(message.Content);
                    Console.WriteLine("=== END LOCAL SDP ===\n");
                };

                // Configure STUN (no hosting required)
                var config = new PeerConnectionConfiguration
                {
                    IceServers = new List<IceServer>
                {
                    new IceServer
                    {
                        Urls = { "stun:stun.l.google.com:19302" } // test only
                    }
                }
                };

                Console.WriteLine("Initializing PeerConnection...");
                await pc.InitializeAsync(config);
                Console.WriteLine("PeerConnection initialized.");

                // Create data channel (reliable, ordered)
                Console.WriteLine("Creating data channel...");
                var dataChannel = await pc.AddDataChannelAsync(
                    "signal_channel",
                    ordered: true,
                    reliable: true);

                dataChannel.StateChanged += () =>
                {
                    Console.WriteLine($"DataChannel state: {dataChannel.State}");
                    if (dataChannel.State == DataChannel.ChannelState.Open)
                    {
                        Console.WriteLine("DataChannel OPEN. Exchanging TCP info...");
                        SendTcpInfo(pc, dataChannel);
                    }
                };

                dataChannel.MessageReceived += msg =>
                {
                    var text = Encoding.UTF8.GetString(msg);
                    Console.WriteLine($"DataChannel received: {text}");

                    if (text.StartsWith("TCP_INFO:"))
                    {
                        try
                        {
                            var info = ParseTcpInfo(text);
                            Console.WriteLine($"Peer TCP info: {info.PublicIP}:{info.PublicPort} (local {info.LocalPort})");
                            BeginTcpHolePunching(info.PublicIP, info.PublicPort, MyTcpPort);
                        }
                        catch (FormatException ex)
                        {
                            Console.WriteLine($"Invalid TCP_INFO received: {ex.Message}");
                        }
                    }
                };

                // Simple manual signaling via console
                if (isOfferSide)
                {
                    // OFFER side
                    Console.WriteLine("Creating SDP offer...");
                    if (!pc.CreateOffer())
                        throw new InvalidOperationException("Failed to create the SDP offer.");

                    Console.WriteLine("Paste ANSWER SDP from remote, then press Enter:");
                    var answerSdp = ReadMultiline();
                    if (string.IsNullOrWhiteSpace(answerSdp))
                        throw new InvalidOperationException("Answer SDP was empty.");

                    var answerMsg = new SdpMessage
                    {
                        Type = SdpMessageType.Answer,
                        Content = answerSdp
                    };
                    await pc.SetRemoteDescriptionAsync(answerMsg);
                }
                else
                {
                    // ANSWER side
                    Console.WriteLine("Paste OFFER SDP from remote, then press Enter:");
                    var offerSdp = ReadMultiline();
                    if (string.IsNullOrWhiteSpace(offerSdp))
                        throw new InvalidOperationException("Offer SDP was empty.");

                    var offerMsg = new SdpMessage
                    {
                        Type = SdpMessageType.Offer,
                        Content = offerSdp
                    };
                    await pc.SetRemoteDescriptionAsync(offerMsg);

                    Console.WriteLine("Creating SDP answer...");
                    if (!pc.CreateAnswer())
                        throw new InvalidOperationException("Failed to create the SDP answer.");
                }

                Console.WriteLine("Waiting for connection... Press Enter to exit.");
                Console.ReadLine();
            }

            // Send our TCP info over WebRTC data channel
            private static void SendTcpInfo(PeerConnection pc, DataChannel dc)
            {
                // Take first ICE candidate as a simple example
                // In a real app, you’d pick the best candidate.
                string publicIP = "0.0.0.0";
                int publicPort = MyTcpPort;

                // MixedReality-WebRTC exposes ICE candidates via events; for simplicity,
                // we just send our local TCP port and let your TCP logic handle details.
                var payload = $"TCP_INFO:LocalPort={MyTcpPort};PublicIP={publicIP};PublicPort={publicPort}";
                var bytes = Encoding.UTF8.GetBytes(payload);
                dc.SendMessage(bytes);
                Console.WriteLine($"Sent TCP info: {payload}");
            }

            private class TcpInfo
            {
                public int LocalPort { get; set; }
                public string PublicIP { get; set; }
                public int PublicPort { get; set; }
            }

            private static TcpInfo ParseTcpInfo(string text)
            {
                if (string.IsNullOrWhiteSpace(text) ||
                    !text.StartsWith("TCP_INFO:", StringComparison.Ordinal))
                    throw new FormatException("Invalid TCP_INFO message.");

                // Format: TCP_INFO:LocalPort=50000;PublicIP=1.2.3.4;PublicPort=49152
                var info = new TcpInfo();
                var body = text.Substring("TCP_INFO:".Length);
                var parts = body.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    int separator = part.IndexOf('=');
                    if (separator <= 0) continue;

                    var key = part.Substring(0, separator).Trim();
                    var value = part.Substring(separator + 1).Trim();
                    switch (key)
                    {
                    case "LocalPort":
                        int localPort;
                        if (!int.TryParse(value, out localPort))
                            throw new FormatException($"Invalid LocalPort value '{value}'.");
                        info.LocalPort = localPort;
                        break;
                    case "PublicIP":
                            info.PublicIP = value;
                            break;
                    case "PublicPort":
                        int publicPort;
                        if (!int.TryParse(value, out publicPort))
                            throw new FormatException($"Invalid PublicPort value '{value}'.");
                        info.PublicPort = publicPort;
                        break;
                }
                }
            if (string.IsNullOrWhiteSpace(info.PublicIP) ||
info.PublicPort < 1 ||
info.PublicPort > 65535)
            {
                throw new FormatException("TCP_INFO does not contain a valid public endpoint.");
            }
            return info;
            }

            // Plug your existing TCP logic here
            private static void BeginTcpHolePunching(string peerPublicIP, int peerPublicPort, int myLocalPort)
            {
                Console.WriteLine($"[TCP] Begin hole punching to {peerPublicIP}:{peerPublicPort} (local {myLocalPort})");

                // TODO: Replace this with your existing TCP solution:
                // - Start TcpListener on myLocalPort
                // - Start TcpClient.ConnectAsync(peerPublicIP, peerPublicPort)
                // - Wait for either incoming or outgoing to succeed
                // - Hand the resulting TcpClient to your existing networking code

                // Example skeleton:
                /*
                var listener = new TcpListener(IPAddress.Any, myLocalPort);
                listener.Start();

                var outgoing = new TcpClient();
                var connectTask = outgoing.ConnectAsync(peerPublicIP, peerPublicPort);
                var incomingTask = listener.AcceptTcpClientAsync();

                var completed = await Task.WhenAny(connectTask, incomingTask);

                TcpClient peer;
                if (completed == incomingTask)
                    peer = incomingTask.Result;
                else
                    peer = outgoing;

                Console.WriteLine("[TCP] Connected to peer!");
                StartMyExistingTcpSession(peer);
                */
            }

            // Read multiline SDP until an empty line
            private static string ReadMultiline()
            {
                var sb = new StringBuilder();
                while (true)
                {
                    var line = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        break;
                    sb.AppendLine(line);
                }
                return sb.ToString();
            }
        }
    }
