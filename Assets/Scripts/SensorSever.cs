using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MQTTnet;
using MQTTnet.Client;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SensorSever : MonoBehaviour {
    private float deltatime = 0;
    [SerializeField]
    GameObject target;
    //[SerializeField]
    //Text dataText;
    //[SerializeField]
    //InputField address; //url
    //[SerializeField]
    //Text consoleText;
    private string serverurl = "http://localhost:3000/";
    //IMqttClient mqttClient;
    [SerializeField]
    Text warningText;
    [SerializeField]
    GameObject spotLight;
    [SerializeField]
    private MqttController mqttController;
    [SerializeField]
    private MqttEulerController mqttEuler;
    NineAxis axisdata;
    Euler eulerdata;
    Euler calibrateEuler = new Euler (); //きゃりぶれデータ
    bool isCalibrate = false;
    [SerializeField]
    GameObject calibratingPanel;
    List<Euler> calEulerList = new List<Euler> ();
    string[] targetStateList = { "平常", "睡眠", "食事" };
    int targetStateNum = 0;
    [SerializeField]
    ToggleGroup toggleGroup;
    [SerializeField]
    Text printStateText;

    void Start () {
        //address.text = serverurl;
        warningText.text = "";
        printStateText.text = "平常時";
        StartCoroutine (ServerTest ());
        //express経由のデータ取得
        //StartCoroutine (GetSensorData ());

        //StartCoroutine (PostServerData ());
        mqttController.setHost ("192.168.1.6");
    }

    // Update is called once per frame
    void Update () {
        deltatime += Time.deltaTime;
        /*
        if (deltatime > 1.0f) {
            //StartCoroutine (GetSensorData ());
            setTarget (axisdata);

            deltatime -= 1.0f;
        }
        */
        if (isCalibrate) {
            //キャリブレーション中 3秒
            if (deltatime > 3.0f) {
                //終了
                CaliburationEnd ();
            }
        }
        if (eulerdata != null) {
            SetTargetEuler (eulerdata);
        }

        //MqttTest();
        mqttController.OnMessageReceived.Subscribe (message => {
            //データ成型
            string json = "";
            string[] arr = message.Split ('{');
            json = string.Join ("{\"", arr);
            string[] semiArr = json.Split (':');
            json = string.Join ("\":", semiArr);
            string[] comArr = json.Split (',');
            json = string.Join (",\"", comArr);
            NineAxis list = JsonUtility.FromJson<NineAxis> (json);
            axisdata = list;
        });
        mqttEuler.OnMessageReceived.Subscribe (message => {
            //データ成型
            string json = "";
            string[] arr = message.Split ('{');
            json = string.Join ("{\"", arr);
            string[] semiArr = json.Split (':');
            json = string.Join ("\":", semiArr);
            string[] comArr = json.Split (',');
            json = string.Join (",\"", comArr);
            Euler data = JsonUtility.FromJson<Euler> (json);
            eulerdata = data;
            if (isCalibrate) {
                calEulerList.Add (data);
            }
        });
    }
    //SensorData取得通信
    IEnumerator GetSensorData () {
        UnityWebRequest req = UnityWebRequest.Get (serverurl + "sensorData");
        req.SetRequestHeader ("key", "KEY");
        yield return req.SendWebRequest ();
        if (req.isNetworkError) {
            Debug.Log (req.error);
        } else {
            if (req.responseCode == 200) {
                //OK
                string jsonText = req.downloadHandler.text;
                //Debug.Log (jsonText);
                List<NineAxis> list = new List<NineAxis> ();
                list = JsonUtility.FromJson<Serialize<NineAxis>> (jsonText).ToList ();
                //Debug.Log (list[0].datetime);
                setTarget (list[0]);
            }
        }
    }
    //9軸オブジェクトへ適用
    void setTarget (NineAxis data) {
        //ax 0-10を想定
        float AX = (data.ax * 18) - 90.0f;
        //ay 0-10を想定
        float AY = (data.ay * 18) + 90.0f;
        //az 0-10を想定
        float AZ = -((data.az * 18) - 90.0f);
        //gx -1-1を想定
        float GX = ((data.gx + 1.0f) * 180) - 90.0f;
        //gy -1-1を想定
        float GY = ((data.gy + 1.0f) * 180);
        //gz -1-1を想定
        float GZ = ((data.gz + 1.0f) * 180);

        //Debug.Log ($"{data.ax:F3}");
        //target.transform.rotation = Quaternion.Euler (90, 180, 0) * (new Quaternion (-data.gx, -data.gy, data.gz, 0));;
        string datastr = "AX:" + $"{data.ax:F3}" + " AY:" + $"{data.ay:F3}" + " AZ:" + $"{data.az:F3}" + "\n";
        datastr += "LX:" + $"{data.lx:F3}" + " LY:" + $"{data.ly:F3}" + " LZ:" + $"{data.lz:F3}" + "\n";
        datastr += "GX:" + $"{data.gx:F3}" + " GY:" + $"{data.gy:F3}" + " GZ:" + $"{data.gz:F3}" + "\n";
        //dataText.text = datastr;
    }
    void SetTargetEuler (Euler euler) {
        float targetPitch = euler.pitch - calibrateEuler.pitch + 90.0f;
        float targetRoll = euler.roll - calibrateEuler.roll;
        target.transform.rotation = Quaternion.Euler (
            targetPitch,
            euler.head - calibrateEuler.head,
            targetRoll);
        if (targetStateNum == 0) {
            //平常時
            if (targetPitch < -30.0f) {
                warningText.text = "Warning!\n危険な体勢です！";
                spotLight.SetActive (true);
            } else {
                warningText.text = "";
                spotLight.SetActive (false);
            }
        }
        //睡眠時
        else if (targetStateNum == 1) {
            if (targetPitch < -30.0f) {
                warningText.text = "Warning!\n危険な体勢です！";
                spotLight.SetActive (true);
            } else {
                warningText.text = "";
                spotLight.SetActive (false);
            }
        } else if (targetStateNum == 2) {
            //食事
            if (targetPitch < -30.0f || targetPitch > 30.0f || targetRoll < -45.0f || targetRoll > 45.0f) {
                warningText.text = "Warning!\n危険な体勢です！";
                spotLight.SetActive (true);
            } else {
                warningText.text = "";
                spotLight.SetActive (false);
            }
        }
    }
    void CaliburationEnd () {
        //キャリブレーション終了
        isCalibrate = false;
        int length = calEulerList.Count;
        float eulerP = 0;
        float eulerH = 0;
        float eulerR = 0;
        foreach (Euler data in calEulerList) {
            eulerP += data.pitch;
            eulerH += data.head;
            eulerR += data.roll;
        }
        calibrateEuler.pitch = eulerP / length;
        calibrateEuler.head = eulerH / length;
        calibrateEuler.roll = eulerR / length;
        //Debug.Log (calibrateEuler.pitch);
        calibratingPanel.SetActive (false);
    }
    //Post通信テスト
    IEnumerator PostServerData () {
        WWWForm form = new WWWForm ();
        //key:data
        form.AddField ("myField", "myData");
        form.AddField ("newxt", 20);

        using (UnityWebRequest req = UnityWebRequest.Post (serverurl + "Post", form)) {
            yield return req.SendWebRequest ();

            if (req.isNetworkError) {
                Debug.Log (req.error);
            } else {
                Debug.Log ("Form upload complete!");
            }
        }
    }
    IEnumerator ServerTest () {
        //consoleText.text = "No Server";
        UnityWebRequest req = UnityWebRequest.Get (serverurl + "tests");
        req.SetRequestHeader ("key", "KEY");
        req.timeout = 10;
        yield return req.SendWebRequest ();
        if (req.isNetworkError) {
            Debug.Log (req.error);
            //consoleText.text = req.error;
        } else {
            if (req.responseCode == 200) {
                //OK
            }
        }
        //consoleText.text = "responceCode:" + req.responseCode.ToString ();
    }
    public void OnClickSetServer () {
        //serverurl = address.text;
        StartCoroutine (ServerTest ());
    }
    public void OnClickCalibration () {
        Debug.Log ("calibrate now");
        deltatime = 0;
        isCalibrate = true;
    }
    public void OnClickSetState () {
        IEnumerable<Toggle> state = toggleGroup.ActiveToggles ();
        Toggle stateToggle = state.FirstOrDefault ();
        //string statetext = stateToggle.GetComponent<Text> ().text;
        Debug.Log (stateToggle);
    }
    public void OnClickStateDefault (GameObject check) {
        Toggle toggle = check.GetComponent<Toggle> ();
        if (toggle.isOn) {
            string text = check.GetComponentsInChildren<Text> ().First ().text;
            for (int i = 0; i < targetStateList.Length; i++) {
                if (text == targetStateList[i]) {
                    targetStateNum = i;
                    printStateText.text = targetStateList[i] + "時";
                }
            }
        } else {
            return;
        }
    }
}