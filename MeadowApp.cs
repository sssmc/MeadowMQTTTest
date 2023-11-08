using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Leds;
using Meadow.Hardware;
using Meadow.Peripherals.Leds;
using System;
using System.Threading.Tasks;

using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet;
using MQTTnet.Client.Subscribing;
using System.Text;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;

namespace MeadowMQTTTest
{
    // Change F7FeatherV2 to F7FeatherV1 for V1.x boards
    public class MeadowApp : App<F7FeatherV2>
    {
        RgbPwmLed onboardLed;

        MqttFactory mqttFactory;
        IMqttClient client;
        MqttClientOptions mqttClientOptions;

        public override Task Initialize()
        {
            Resolver.Log.Info("Initialize...");

            onboardLed = new RgbPwmLed(
                redPwmPin: Device.Pins.OnboardLedRed,
                greenPwmPin: Device.Pins.OnboardLedGreen,
                bluePwmPin: Device.Pins.OnboardLedBlue,
                CommonType.CommonAnode);

            try
            {
                var wifi = Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();
                wifi.Connect("White Rabbit", "2511560A7196", TimeSpan.FromSeconds(45));
                wifi.NetworkConnected += (networkAdapter, networkConnectionEventArgs) =>
                {
                    Console.WriteLine("Joined network");
                    Console.WriteLine($"IP Address: {networkAdapter.IpAddress}.");
                    Console.WriteLine($"Subnet mask: {networkAdapter.SubnetMask}");
                    Console.WriteLine($"Gateway: {networkAdapter.Gateway}");
                };
            }
            catch (Exception ex)
            {
                Resolver.Log.Error($"Failed to Connect: {ex.Message}");
            }

            mqttFactory = new MqttFactory();
            client = mqttFactory.CreateMqttClient();
            mqttClientOptions = (MqttClientOptions) new MqttClientOptionsBuilder()
                                    .WithClientId(Guid.NewGuid().ToString())
                                    .WithTcpServer("mqtt3.thingspeak.com", 1883)
                                    .WithClientId("FDkPCxA2KTkHMgANKik6NgI")
                                    .WithCredentials("FDkPCxA2KTkHMgANKik6NgI", "lRBFHoyhV9ruKuh0sy7s0QXm")
                                    .WithCleanSession()
                                    .Build();

            client.UseConnectedHandler(Client_ConnectedAsync);
            client.UseDisconnectedHandler(Client_DisconnectedAsync);
            client.ConnectAsync(mqttClientOptions);


            return base.Initialize();
        }

        private async Task Client_ConnectedAsync(MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Connected to MQTT server");
            var topicFilter = new MqttTopicFilterBuilder()
                                .WithTopic("channels/2328115/subscribe/fields/+")
                                .Build();
            await client.SubscribeAsync(topicFilter);
            client.UseApplicationMessageReceivedHandler(Client_ApplicationMessageReceivedHandler);
        }

        private async Task Client_DisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Disconnected from MQTT server");
            await Task.Delay(TimeSpan.FromSeconds(5));
            try
            {
                await client.ConnectAsync(mqttClientOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reconnect failed {ex.Message}");
            }
        }

        private Task Client_ApplicationMessageReceivedHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            Console.WriteLine($"Message received on topic {e.ApplicationMessage.Topic}");
            Console.WriteLine($"Message: {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
            return Task.CompletedTask;
        }


        public override Task Run()
        {
            Resolver.Log.Info("Run...");

            return base.Run();
        }

    }
}