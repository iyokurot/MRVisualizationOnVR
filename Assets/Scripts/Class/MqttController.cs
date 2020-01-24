using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using MQTTnet;
using MQTTnet.Client;
using UniRx;

public class MqttController : MonoBehaviour {
    IMqttClient mqttClient;
    public Subject<string> OnMessageReceived { get; private set; } = new Subject<string> ();
    private string mqttHost="192.168.1.6";
    async void Start ()
    {
        var factory = new MqttFactory ();
        mqttClient = factory.CreateMqttClient ();

        var options = new MqttClientOptionsBuilder ()
            .WithTcpServer (mqttHost, 1883)
            .WithClientId ("Unity.client.subscriber")//Guid.NewGuid ().ToString ())
            //.WithCredentials ("your_MQTT_username", "your_MQTT_password")
            //.WithTls ()
            .Build ();

        mqttClient.Connected += async (s, e) =>
        {
            Debug.Log ("MQTTブローカに接続しました");
            await mqttClient.SubscribeAsync (
                new TopicFilterBuilder ()
                .WithTopic ("itoyuNineAxis")
                .Build ());
            Debug.Log ("指定したトピックをSubscribeしました");
        };

        mqttClient.Disconnected += async (s, e) =>
        {
            if (e.Exception == null)
            {
                Debug.Log ("サーバとの通信を切断しました");
                return;
            }

            Debug.Log ("サーバから切断されました。");
        };

        mqttClient.ApplicationMessageReceived += (s, e) =>
        {
            var message = System.Text.Encoding.UTF8.GetString (e.ApplicationMessage.Payload);
            //Debug.Log ($"メッセージ受信 : {message}");
                OnMessageReceived.OnNext (message);
            
        };

        await mqttClient.ConnectAsync (options);
    }

    async private void OnApplicationQuit ()
    {
        await mqttClient.DisconnectAsync ();
    }
    public void setHost(string host){
        mqttHost=host;
    }
}