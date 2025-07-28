using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class XunfeiSpeechToText : MonoBehaviour
{
    #region 参数
    /// <summary>
    /// host地址
    /// </summary>
    [SerializeField] private string m_HostUrl = "iat-api.xfyun.cn";
    /// <summary>
    /// 语言
    /// </summary>
    [SerializeField] private string m_Language = "zh_cn";
    /// <summary>
    /// 应用领域
    /// </summary>
    [SerializeField] private string m_Domain = "iat";
    /// <summary>
    /// 方言mandarin：中文普通话、其他语种
    /// </summary>
    [SerializeField] private string m_Accent = "mandarin";
    /// <summary>
    /// 音频的采样率
    /// </summary>
    [SerializeField] private string m_Format = "audio/L16;rate=16000";
    /// <summary>
    /// 音频数据格式
    /// </summary>
    [SerializeField] private string m_Encoding = "raw";

    /// <summary>
    /// websocket
    /// </summary>
    private ClientWebSocket m_WebSocket;
    /// <summary>
    /// 传输中断标记点
    /// </summary>
    private CancellationToken m_CancellationToken;
    /// <summary>
    /// 语音识别API地址
    /// </summary>
    [SerializeField]
    [Header("语音识别API地址")]
    private string m_SpeechRecognizeURL;
    /// <summary>
    /// 讯飞的AppID
    /// </summary>
    [Header("填写APP ID")]
    [SerializeField] private string m_AppID = "讯飞的AppID";
    /// <summary>
    /// 讯飞的APIKey
    /// </summary>
    [Header("填写Api Key")]
    [SerializeField] private string m_APIKey = "讯飞的APIKey";
    /// <summary>
    /// 讯飞的APISecret
    /// </summary>
    [Header("填写Secret Key")]
    [SerializeField] private string m_APISecret = "讯飞的APISecret";
    /// <summary>
    /// 计算方法调用的时间
    /// </summary>
    [SerializeField] protected System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    #endregion
    private void Awake()
    {
        //注册按钮事件
        RegistButtonEvent();
        //绑定地址 地址就是讯飞平台上的 WebSocket API地址
        m_SpeechRecognizeURL = "wss://iat-api.xfyun.cn/v2/iat";
    }

    #region 语音输入
    /// <summary>
    /// 语音输入的按钮
    /// </summary>
    [SerializeField] private Button m_VoiceInputBotton;
    /// <summary>
    /// 录音按钮的文本
    /// </summary>
    [SerializeField] private TextMeshProUGUI m_VoiceBottonText;
    /// <summary>
    /// 录音的提示信息
    /// </summary>
    [SerializeField] private Text m_RecordTips;

    /// <summary>
    /// 录制的音频长度
    /// </summary>
    public int m_RecordingLength = 5;

    /// <summary>
    /// 临时接收音频的片段
    /// </summary>
    private AudioClip recording;

    /// <summary>
    /// 注册按钮事件
    /// </summary>
    private void RegistButtonEvent()
    {
        if (m_VoiceInputBotton == null || m_VoiceInputBotton.GetComponent<EventTrigger>())
            return;
        EventTrigger _trigger = m_VoiceInputBotton.gameObject.AddComponent<EventTrigger>();
        //添加按钮按下的事件
        EventTrigger.Entry _pointDown_entry = new EventTrigger.Entry();
        _pointDown_entry.eventID = EventTriggerType.PointerDown;
        _pointDown_entry.callback = new EventTrigger.TriggerEvent();
        //添加按钮松开事件
        EventTrigger.Entry _pointUp_entry = new EventTrigger.Entry();
        _pointUp_entry.eventID = EventTriggerType.PointerUp;
        _pointUp_entry.callback = new EventTrigger.TriggerEvent();
        //添加委托事件
        _pointDown_entry.callback.AddListener(delegate { StartRecord(); });
        _pointUp_entry.callback.AddListener(delegate { StopRecord(); });
        _trigger.triggers.Add(_pointDown_entry);
        _trigger.triggers.Add(_pointUp_entry);
    }

    /// <summary>
    /// 开始录制
    /// </summary>
    public void StartRecord()
    {
        m_VoiceBottonText.text = "正在录音中...";
        StartRecordAudio();
    }
    /// <summary>
    /// 结束录制
    /// </summary>
    public void StopRecord()
    {
        m_VoiceBottonText.text = "按住按钮，开始录音";
        m_RecordTips.text = "录音结束，正在识别...";
        StopRecordAudio(AcceptClip);
    }

    /// <summary>
    /// 开始录制声音
    /// </summary>
    public void StartRecordAudio()
    {
        recording = Microphone.Start(null, true, m_RecordingLength, 16000);
    }

    /// <summary>
    /// 结束录制，返回audioClip
    /// </summary>
    /// <param name="_callback"></param>
    public void StopRecordAudio(Action<AudioClip> _callback)
    {
        Microphone.End(null);
        _callback(recording);
    }

    /// <summary>
    /// 处理录制的音频数据
    /// </summary>
    /// <param name="_data"></param>
    public void AcceptClip(AudioClip _audioClip)
    {

        m_RecordTips.text = "正在进行语音识别...";
        SpeechToText(_audioClip, DealingTextCallback);
    }

    /// <summary>
    /// 处理识别到的文本
    /// </summary>
    /// <param name="_msg"></param>
    private void DealingTextCallback(string _msg)
    {
        //在此处处理接收到的数据，可以选择发送给大模型，或者打印测试，会在后续补充功能
        m_RecordTips.text = _msg;
        Debug.Log(_msg);
    }


    #endregion

    #region 获取鉴权Url

    /// <summary>
    /// 获取鉴权url
    /// </summary>
    /// <returns></returns>
    private string GetUrl()
    {
        //获取时间戳
        string date = DateTime.Now.ToString("r");
        //拼接原始的signature
        string signature_origin = string.Format("host: " + m_HostUrl + "\ndate: " + date + "\nGET /v2/iat HTTP/1.1");
        //hmac-sha256算法-签名，并转换为base64编码
        string signature = Convert.ToBase64String(new HMACSHA256(Encoding.UTF8.GetBytes(m_APISecret)).ComputeHash(Encoding.UTF8.GetBytes(signature_origin)));
        //拼接原始的authorization
        string authorization_origin = string.Format("api_key=\"{0}\",algorithm=\"hmac-sha256\",headers=\"host date request-line\",signature=\"{1}\"", m_APIKey, signature);
        //转换为base64编码
        string authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization_origin));
        //拼接鉴权的url
        string url = string.Format("{0}?authorization={1}&date={2}&host={3}", m_SpeechRecognizeURL, authorization, date, m_HostUrl);

        return url;
    }

    #endregion

    #region 语音识别

    /// <summary>
    /// 语音识别
    /// </summary>
    /// <param name="_clip"></param>
    /// <param name="_callback"></param>
    public void SpeechToText(AudioClip _clip, Action<string> _callback)
    {
        byte[] _audioData = ConvertClipToBytes(_clip);
        StartCoroutine(SendAudioData(_audioData, _callback));
    }
    /// <summary>
    /// 识别短文本
    /// </summary>
    /// <param name="_audioData"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    public IEnumerator SendAudioData(byte[] _audioData, Action<string> _callback)
    {
        yield return null;
        ConnetHostAndRecognize(_audioData, _callback);
    }

    /// <summary>
    /// 连接服务，开始识别
    /// </summary>
    /// <param name="_audioData"></param>
    /// <param name="_callback"></param>
    private async void ConnetHostAndRecognize(byte[] _audioData, Action<string> _callback)
    {
        try
        {
            stopwatch.Restart();
            //建立socket连接
            m_WebSocket = new ClientWebSocket();
            m_CancellationToken = new CancellationToken();
            Uri uri = new Uri(GetUrl());
            await m_WebSocket.ConnectAsync(uri, m_CancellationToken);
            //开始识别
            SendVoiceData(_audioData, m_WebSocket);
            StringBuilder stringBuilder = new StringBuilder();
            while (m_WebSocket.State == WebSocketState.Open)
            {
                var result = new byte[4096];
                await m_WebSocket.ReceiveAsync(new ArraySegment<byte>(result), m_CancellationToken);
                //去除空字节
                List<byte> list = new List<byte>(result); while (list[list.Count - 1] == 0x00) list.RemoveAt(list.Count - 1);
                string str = Encoding.UTF8.GetString(list.ToArray());
                //获取返回的json
                ResponseData _responseData = JsonUtility.FromJson<ResponseData>(str);
                if (_responseData.code == 0)
                {
                    stringBuilder.Append(GetWords(_responseData));
                }
                else
                {
                    PrintErrorLog(_responseData.code);

                }
                m_WebSocket.Abort();
            }

            string _resultMsg = stringBuilder.ToString();

            //识别成功，回调
            _callback(_resultMsg);

            stopwatch.Stop();
            if (_resultMsg.Equals(null) || _resultMsg.Equals(""))
            {
                Debug.Log("语音识别为空字符串");
            }
            else
            {
                //识别的数据不为空 在此处做功能处理

            }
            Debug.Log("讯飞语音识别耗时：" + stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            Debug.LogError("报错信息: " + ex.Message);
            m_WebSocket.Dispose();
        }
    }

    /// <summary>
    /// 获取识别到的文本
    /// </summary>
    /// <param name="_responseData"></param>
    /// <returns></returns>
    private string GetWords(ResponseData _responseData)
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var item in _responseData.data.result.ws)
        {
            foreach (var _cw in item.cw)
            {
                stringBuilder.Append(_cw.w);
            }
        }

        return stringBuilder.ToString();
    }


    private void SendVoiceData(byte[] audio, ClientWebSocket socket)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }
        PostData _postData = new PostData()
        {
            common = new CommonTag(m_AppID),
            business = new BusinessTag(m_Language, m_Domain, m_Accent),
            data = new DataTag(2, m_Format, m_Encoding, Convert.ToBase64String(audio))
        };

        string _jsonData = JsonUtility.ToJson(_postData);

        //发送数据
        socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(_jsonData)), WebSocketMessageType.Binary, true, new CancellationToken());
    }

    #endregion

    #region 工具方法
    /// <summary>
    /// audioclip转为byte[]
    /// </summary>
    /// <param name="audioClip"></param>
    /// <returns></returns>
    public byte[] ConvertClipToBytes(AudioClip audioClip)
    {
        float[] samples = new float[audioClip.samples];

        audioClip.GetData(samples, 0);

        short[] intData = new short[samples.Length];

        byte[] bytesData = new byte[samples.Length * 2];

        int rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = new byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        return bytesData;
    }

    /// <summary>
    /// 打印错误日志
    /// </summary>
    /// <param name="status"></param>
    private void PrintErrorLog(int status)
    {
        if (status == 10005)
        {
            Debug.LogError("appid授权失败");
            return;
        }
        if (status == 10006)
        {
            Debug.LogError("请求缺失必要参数");
            return;
        }
        if (status == 10007)
        {
            Debug.LogError("请求的参数值无效");
            return;
        }
        if (status == 10010)
        {
            Debug.LogError("引擎授权不足");
            return;
        }
        if (status == 10019)
        {
            Debug.LogError("session超时");
            return;
        }
        if (status == 10043)
        {
            Debug.LogError("音频解码失败");
            return;
        }
        if (status == 10101)
        {
            Debug.LogError("引擎会话已结束");
            return;
        }
        if (status == 10313)
        {
            Debug.LogError("appid不能为空");
            return;
        }
        if (status == 10317)
        {
            Debug.LogError("版本非法");
            return;
        }
        if (status == 11200)
        {
            Debug.LogError("没有权限");
            return;
        }
        if (status == 11201)
        {
            Debug.LogError("日流控超限");
            return;
        }
        if (status == 10160)
        {
            Debug.LogError("请求数据格式非法");
            return;
        }
        if (status == 10161)
        {
            Debug.LogError("base64解码失败");
            return;
        }
        if (status == 10163)
        {
            Debug.LogError("缺少必传参数，或者参数不合法，具体原因见详细的描述");
            return;
        }
        if (status == 10200)
        {
            Debug.LogError("读取数据超时");
            return;
        }
        if (status == 10222)
        {
            Debug.LogError("网络异常");
            return;
        }
    }


    #endregion

    #region 数据定义

    /// <summary>
    /// 发送的数据
    /// </summary>
    [Serializable]
    public class PostData
    {
        [SerializeField] public CommonTag common;
        [SerializeField] public BusinessTag business;
        [SerializeField] public DataTag data;
    }

    [Serializable]
    public class CommonTag
    {
        [SerializeField] public string app_id = string.Empty;
        public CommonTag(string app_id)
        {
            this.app_id = app_id;
        }
    }
    [Serializable]
    public class BusinessTag
    {
        [SerializeField] public string language = "zh_cn";
        [SerializeField] public string domain = "iat";
        [SerializeField] public string accent = "mandarin";
        public BusinessTag(string language, string domain, string accent)
        {
            this.language = language;
            this.domain = domain;
            this.accent = accent;
        }
    }

    [Serializable]
    public class DataTag
    {
        [SerializeField] public int status = 2;
        [SerializeField] public string format = "audio/L16;rate=16000";
        [SerializeField] public string encoding = "raw";
        [SerializeField] public string audio = string.Empty;
        public DataTag(int status, string format, string encoding, string audio)
        {
            this.status = status;
            this.format = format;
            this.encoding = encoding;
            this.audio = audio;
        }
    }


    [Serializable]
    public class ResponseData
    {
        [SerializeField] public int code = 0;
        [SerializeField] public string message = string.Empty;
        [SerializeField] public string sid = string.Empty;
        [SerializeField] public ResponcsedataTag data;
    }

    [Serializable]
    public class ResponcsedataTag
    {
        [SerializeField] public Results result;
        [SerializeField] public int status = 2;
    }

    [Serializable]
    public class Results
    {
        [SerializeField] public List<WsTag> ws;
    }

    [Serializable]
    public class WsTag
    {
        [SerializeField] public List<CwTag> cw;
    }

    [Serializable]
    public class CwTag
    {
        [SerializeField] public int sc = 0;
        [SerializeField] public string w = string.Empty;
    }

    #endregion

}
