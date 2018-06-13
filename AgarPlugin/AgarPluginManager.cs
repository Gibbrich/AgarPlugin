using System;
using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;

namespace AgarPlugin
{
    public class AgarPluginManager : Plugin
    {
        private const float MAP_WIDTH = 20;

        public override Version Version
        {
            get { return new Version(0, 1, 1, 1); }
        }

        public override bool ThreadSafe
        {
            get { return false; }
        }

        private readonly Dictionary<IClient, Player> players = new Dictionary<IClient, Player>();

        public AgarPluginManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
        }

        private void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            var newPlayer = CreatePlayer(e);
            NotifyPlayersNewPlayerConnected(e, newPlayer);
            players.Add(e.Client, newPlayer);
            SendDataAboutExistingPlayersToNewPlayer(e);

            e.Client.MessageReceived += MovementMessageReceived;
        }

        private void SendDataAboutExistingPlayersToNewPlayer(ClientConnectedEventArgs e)
        {
            using (var playerWriter = DarkRiftWriter.Create())
            {
                foreach (var player in players.Values)
                {
                    playerWriter.Write(player.ID);
                    playerWriter.Write(player.X);
                    playerWriter.Write(player.Y);
                    playerWriter.Write(player.Radius);
                    playerWriter.Write(player.ColorR);
                    playerWriter.Write(player.ColorG);
                    playerWriter.Write(player.ColorB);
                }

                using (var playerMessage = Message.Create(Tags.SPAWN_PLAYER, playerWriter))
                {
                    e.Client.SendMessage(playerMessage, SendMode.Reliable);
                }
            }
        }

        private void NotifyPlayersNewPlayerConnected(ClientConnectedEventArgs e, Player newPlayer)
        {
            using (var newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayer.ID);
                newPlayerWriter.Write(newPlayer.X);
                newPlayerWriter.Write(newPlayer.Y);
                newPlayerWriter.Write(newPlayer.Radius);
                newPlayerWriter.Write(newPlayer.ColorR);
                newPlayerWriter.Write(newPlayer.ColorG);
                newPlayerWriter.Write(newPlayer.ColorB);

                using (var newPlayerMessage = Message.Create(Tags.SPAWN_PLAYER, newPlayerWriter))
                {
                    ClientManager
                        .GetAllClients()
                        .Where(x => x != e.Client)
                        .ForEach(client => client.SendMessage(newPlayerMessage, SendMode.Reliable));
                }
            }
        }

        private static Player CreatePlayer(ClientConnectedEventArgs e)
        {
            var r = new Random();
            var newPlayer = new Player(
                e.Client.ID,
                (float) r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2,
                (float) r.NextDouble() * MAP_WIDTH - MAP_WIDTH / 2,
                1f,
                (byte) r.Next(0, 200),
                (byte) r.Next(0, 200),
                (byte) r.Next(0, 200)
            );
            return newPlayer;
        }

        private void MovementMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                if (message.Tag != Tags.MOVE_PLAYER)
                {
                    return;
                }
                
                using (var reader = message.GetReader())
                {
                    var newX = reader.ReadSingle();
                    var newY = reader.ReadSingle();
                    var player = players[e.Client];
                    player.X = newX;
                    player.Y = newY;

                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write(player.ID);
                        writer.Write(player.X);
                        writer.Write(player.Y);
                        message.Serialize(writer);
                    }

                    ClientManager
                        .GetAllClients()
                        .Where(x => x != e.Client)
                        .ForEach(client => client.SendMessage(message, e.SendMode));
                }
            }
        }
    }
}