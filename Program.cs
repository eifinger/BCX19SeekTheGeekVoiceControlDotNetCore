using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Intent;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Newtonsoft.Json.Linq;

namespace BCX19SeekTheGeekVoiceControlDotNetCore
{
    class Program
    {
        static MqttClient mqttClient;

        static string mqttBrokerAddress = "broker.hivemq.com";

        static string mqttUsername = "";

        static string mqttPassword = "";

        static Dictionary<string, int> nameToIdDict = new Dictionary<string, int>
        {
            { "patrick", 1 },
            { "kevin", 2 },
            { "khang", 3 },
            { "robert", 4 },
            { "denis", 5 },
            { "stansilav", 6 },
            { "georg", 7 }
        };

    public static async Task RecognizeSpeechAsync()
        {
            initMqttClient(mqttBrokerAddress);
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key // and service region (e.g., "westus").
            var intentConfig = SpeechConfig.FromSubscription("", "westus2");

            // Creates a speech recognizer.
            using (var intentRecognizer = new IntentRecognizer(intentConfig))
            {
                // The TaskCompletionSource to stop recognition.
                var stopRecognition = new TaskCompletionSource<int>();

                var model = LanguageUnderstandingModel.FromAppId("");
                intentRecognizer.AddAllIntents(model);

                // Subscribes to events.
                intentRecognizer.Recognizing += (s, e) => {
                    Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                };

                intentRecognizer.Recognized += (s, e) => {
                    if (e.Result.Reason == ResultReason.RecognizedIntent)
                    {
                        Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                        Console.WriteLine($"    Intent Id: {e.Result.IntentId}.");
                        Console.WriteLine($"    Language Understanding JSON: {e.Result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult)}.");
                        if(e.Result.IntentId == "FollowPerson")
                        {
                            var jsonResult = e.Result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult);
                            dynamic stuff = JObject.Parse(jsonResult);
                            try
                            {
                                string name = stuff.entities[0].entity;
                                Console.WriteLine(name);
                                int id = nameToIdDict.GetValueOrDefault(name);
                                mqttClient.Publish("bcx19-seek-the-geek/tag/control", Encoding.UTF8.GetBytes($"target.{name}"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                                Console.WriteLine("MQTT Message sent");
                            }
                            catch
                            {
                                Console.WriteLine("Error");
                            }
                        }
                        else if(e.Result.IntentId == "Rover.Stop")
                        {
                            mqttClient.Publish("bcx19-seek-the-geek/tag/control", Encoding.UTF8.GetBytes("rover.stop"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                            Console.WriteLine("MQTT Message sent");
                        }
                        else if (e.Result.IntentId == "Rover.Start")
                        {
                            mqttClient.Publish("bcx19-seek-the-geek/tag/control", Encoding.UTF8.GetBytes("rover.start"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                            Console.WriteLine("MQTT Message sent");
                        }
                        else if (e.Result.IntentId == "Rover.Left")
                        {
                            mqttClient.Publish("bcx19-seek-the-geek/tag/control", Encoding.UTF8.GetBytes("rover.left"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                            Console.WriteLine("MQTT Message sent");
                        }
                        else if (e.Result.IntentId == "Rover.Right")
                        {
                            mqttClient.Publish("bcx19-seek-the-geek/tag/control", Encoding.UTF8.GetBytes("rover.right"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                            Console.WriteLine("MQTT Message sent");
                        }
                        else if (e.Result.IntentId == "Rover.Exit")
                        {
                            mqttClient.Publish("bcx19-seek-the-geek/tag/control", Encoding.UTF8.GetBytes("rover.exit"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                            Console.WriteLine("MQTT Message sent");
                        }
                        else if (e.Result.IntentId == "Rover.Back")
                        {
                            mqttClient.Publish("bcx19-seek-the-geek/tag/control", Encoding.UTF8.GetBytes("rover.back"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                            Console.WriteLine("MQTT Message sent");
                        }
                    }
                    else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                        Console.WriteLine($"    Intent not recognized.");
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                intentRecognizer.Canceled += (s, e) => {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }

                    stopRecognition.TrySetResult(0);
                };

                intentRecognizer.SessionStarted += (s, e) => {
                    Console.WriteLine("\n    Session started event.");
                };

                intentRecognizer.SessionStopped += (s, e) => {
                    Console.WriteLine("\n    Session stopped event.");
                    Console.WriteLine("\nStop recognition.");
                    stopRecognition.TrySetResult(0);
                };


                // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                Console.WriteLine("Say something...");
                await intentRecognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                // Waits for completion.
                // Use Task.WaitAny to keep the task rooted.
                Task.WaitAny(new[] { stopRecognition.Task });

                // Stops recognition.
                await intentRecognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }

        static void initMqttClient(string hostname)
        {
            // Create a new MQTT client.
            mqttClient = new MqttClient(hostname);

            string clientId = Guid.NewGuid().ToString();
            mqttClient.Connect(clientId, mqttUsername, mqttPassword);
        }

        static void Main()
        {
            RecognizeSpeechAsync().Wait();
            Console.WriteLine("Please press a key to continue.");
            Console.ReadLine();
        }
    }
}
