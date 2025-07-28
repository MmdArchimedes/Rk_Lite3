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
    #region ����
    /// <summary>
    /// host��ַ
    /// </summary>
    [SerializeField] private string m_HostUrl = "iat-api.xfyun.cn";
    /// <summary>
    /// ����
    /// </summary>
    [SerializeField] private string m_Language = "zh_cn";
    /// <summary>
    /// Ӧ������
    /// </summary>
    [SerializeField] private string m_Domain = "iat";
    /// <summary>
    /// ����mandarin��������ͨ������������
    /// </summary>
    [SerializeField] private string m_Accent = "mandarin";
    /// <summary>
    /// ��Ƶ�Ĳ�����
    /// </summary>
    [SerializeField] private string m_Format = "audio/L16;rate=16000";
    /// <summary>
    /// ��Ƶ���ݸ�ʽ
    /// </summary>
    [SerializeField] private string m_Encoding = "raw";

    /// <summary>
    /// websocket
    /// </summary>
    private ClientWebSocket m_WebSocket;
    /// <summary>
    /// �����жϱ�ǵ�
    /// </summary>
    private CancellationToken m_CancellationToken;
    /// <summary>
    /// ����ʶ��API��ַ
    /// </summary>
    [SerializeField]
    [Header("����ʶ��API��ַ")]
    private string m_SpeechRecognizeURL;
    /// <summary>
    /// Ѷ�ɵ�AppID
    /// </summary>
    [Header("��дAPP ID")]
    [SerializeField] private string m_AppID = "Ѷ�ɵ�AppID";
    /// <summary>
    /// Ѷ�ɵ�APIKey
    /// </summary>
    [Header("��дApi Key")]
    [SerializeField] private string m_APIKey = "Ѷ�ɵ�APIKey";
    /// <summary>
    /// Ѷ�ɵ�APISecret
    /// </summary>
    [Header("��дSecret Key")]
    [SerializeField] private string m_APISecret = "Ѷ�ɵ�APISecret";
    /// <summary>
    /// ���㷽�����õ�ʱ��
    /// </summary>
    [SerializeField] protected System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    #endregion
    private void Awake()
    {
        //ע�ᰴť�¼�
        RegistButtonEvent();
        //�󶨵�ַ ��ַ����Ѷ��ƽ̨�ϵ� WebSocket API��ַ
        m_SpeechRecognizeURL = "wss://iat-api.xfyun.cn/v2/iat";
    }

    #region ��������
    /// <summary>
    /// ��������İ�ť
    /// </summary>
    [SerializeField] private Button m_VoiceInputBotton;
    /// <summary>
    /// ¼����ť���ı�
    /// </summary>
    [SerializeField] private TextMeshProUGUI m_VoiceBottonText;
    /// <summary>
    /// ¼������ʾ��Ϣ
    /// </summary>
    [SerializeField] private Text m_RecordTips;

    /// <summary>
    /// ¼�Ƶ���Ƶ����
    /// </summary>
    public int m_RecordingLength = 5;

    /// <summary>
    /// ��ʱ������Ƶ��Ƭ��
    /// </summary>
    private AudioClip recording;

    /// <summary>
    /// ע�ᰴť�¼�
    /// </summary>
    private void RegistButtonEvent()
    {
        if (m_VoiceInputBotton == null || m_VoiceInputBotton.GetComponent<EventTrigger>())
            return;
        EventTrigger _trigger = m_VoiceInputBotton.gameObject.AddComponent<EventTrigger>();
        //��Ӱ�ť���µ��¼�
        EventTrigger.Entry _pointDown_entry = new EventTrigger.Entry();
        _pointDown_entry.eventID = EventTriggerType.PointerDown;
        _pointDown_entry.callback = new EventTrigger.TriggerEvent();
        //��Ӱ�ť�ɿ��¼�
        EventTrigger.Entry _pointUp_entry = new EventTrigger.Entry();
        _pointUp_entry.eventID = EventTriggerType.PointerUp;
        _pointUp_entry.callback = new EventTrigger.TriggerEvent();
        //���ί���¼�
        _pointDown_entry.callback.AddListener(delegate { StartRecord(); });
        _pointUp_entry.callback.AddListener(delegate { StopRecord(); });
        _trigger.triggers.Add(_pointDown_entry);
        _trigger.triggers.Add(_pointUp_entry);
    }

    /// <summary>
    /// ��ʼ¼��
    /// </summary>
    public void StartRecord()
    {
        m_VoiceBottonText.text = "����¼����...";
        StartRecordAudio();
    }
    /// <summary>
    /// ����¼��
    /// </summary>
    public void StopRecord()
    {
        m_VoiceBottonText.text = "��ס��ť����ʼ¼��";
        m_RecordTips.text = "¼������������ʶ��...";
        StopRecordAudio(AcceptClip);
    }

    /// <summary>
    /// ��ʼ¼������
    /// </summary>
    public void StartRecordAudio()
    {
        recording = Microphone.Start(null, true, m_RecordingLength, 16000);
    }

    /// <summary>
    /// ����¼�ƣ�����audioClip
    /// </summary>
    /// <param name="_callback"></param>
    public void StopRecordAudio(Action<AudioClip> _callback)
    {
        Microphone.End(null);
        _callback(recording);
    }

    /// <summary>
    /// ����¼�Ƶ���Ƶ����
    /// </summary>
    /// <param name="_data"></param>
    public void AcceptClip(AudioClip _audioClip)
    {

        m_RecordTips.text = "���ڽ�������ʶ��...";
        SpeechToText(_audioClip, DealingTextCallback);
    }

    /// <summary>
    /// ����ʶ�𵽵��ı�
    /// </summary>
    /// <param name="_msg"></param>
    private void DealingTextCallback(string _msg)
    {
        //�ڴ˴�������յ������ݣ�����ѡ���͸���ģ�ͣ����ߴ�ӡ���ԣ����ں������书��
        m_RecordTips.text = _msg;
        Debug.Log(_msg);
    }


    #endregion

    #region ��ȡ��ȨUrl

    /// <summary>
    /// ��ȡ��Ȩurl
    /// </summary>
    /// <returns></returns>
    private string GetUrl()
    {
        //��ȡʱ���
        string date = DateTime.Now.ToString("r");
        //ƴ��ԭʼ��signature
        string signature_origin = string.Format("host: " + m_HostUrl + "\ndate: " + date + "\nGET /v2/iat HTTP/1.1");
        //hmac-sha256�㷨-ǩ������ת��Ϊbase64����
        string signature = Convert.ToBase64String(new HMACSHA256(Encoding.UTF8.GetBytes(m_APISecret)).ComputeHash(Encoding.UTF8.GetBytes(signature_origin)));
        //ƴ��ԭʼ��authorization
        string authorization_origin = string.Format("api_key=\"{0}\",algorithm=\"hmac-sha256\",headers=\"host date request-line\",signature=\"{1}\"", m_APIKey, signature);
        //ת��Ϊbase64����
        string authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization_origin));
        //ƴ�Ӽ�Ȩ��url
        string url = string.Format("{0}?authorization={1}&date={2}&host={3}", m_SpeechRecognizeURL, authorization, date, m_HostUrl);

        return url;
    }

    #endregion

    #region ����ʶ��

    /// <summary>
    /// ����ʶ��
    /// </summary>
    /// <param name="_clip"></param>
    /// <param name="_callback"></param>
    public void SpeechToText(AudioClip _clip, Action<string> _callback)
    {
        byte[] _audioData = ConvertClipToBytes(_clip);
        StartCoroutine(SendAudioData(_audioData, _callback));
    }
    /// <summary>
    /// ʶ����ı�
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
    /// ���ӷ��񣬿�ʼʶ��
    /// </summary>
    /// <param name="_audioData"></param>
    /// <param name="_callback"></param>
    private async void ConnetHostAndRecognize(byte[] _audioData, Action<string> _callback)
    {
        try
        {
            stopwatch.Restart();
            //����socket����
            m_WebSocket = new ClientWebSocket();
            m_CancellationToken = new CancellationToken();
            Uri uri = new Uri(GetUrl());
            await m_WebSocket.ConnectAsync(uri, m_CancellationToken);
            //��ʼʶ��
            SendVoiceData(_audioData, m_WebSocket);
            StringBuilder stringBuilder = new StringBuilder();
            while (m_WebSocket.State == WebSocketState.Open)
            {
                var result = new byte[4096];
                await m_WebSocket.ReceiveAsync(new ArraySegment<byte>(result), m_CancellationToken);
                //ȥ�����ֽ�
                List<byte> list = new List<byte>(result); while (list[list.Count - 1] == 0x00) list.RemoveAt(list.Count - 1);
                string str = Encoding.UTF8.GetString(list.ToArray());
                //��ȡ���ص�json
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

            //ʶ��ɹ����ص�
            _callback(_resultMsg);

            stopwatch.Stop();
            if (_resultMsg.Equals(null) || _resultMsg.Equals(""))
            {
                Debug.Log("����ʶ��Ϊ���ַ���");
            }
            else
            {
                //ʶ������ݲ�Ϊ�� �ڴ˴������ܴ���

            }
            Debug.Log("Ѷ������ʶ���ʱ��" + stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            Debug.LogError("������Ϣ: " + ex.Message);
            m_WebSocket.Dispose();
        }
    }

    /// <summary>
    /// ��ȡʶ�𵽵��ı�
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

        //��������
        socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(_jsonData)), WebSocketMessageType.Binary, true, new CancellationToken());
    }

    #endregion

    #region ���߷���
    /// <summary>
    /// audioclipתΪbyte[]
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
    /// ��ӡ������־
    /// </summary>
    /// <param name="status"></param>
    private void PrintErrorLog(int status)
    {
        if (status == 10005)
        {
            Debug.LogError("appid��Ȩʧ��");
            return;
        }
        if (status == 10006)
        {
            Debug.LogError("����ȱʧ��Ҫ����");
            return;
        }
        if (status == 10007)
        {
            Debug.LogError("����Ĳ���ֵ��Ч");
            return;
        }
        if (status == 10010)
        {
            Debug.LogError("������Ȩ����");
            return;
        }
        if (status == 10019)
        {
            Debug.LogError("session��ʱ");
            return;
        }
        if (status == 10043)
        {
            Debug.LogError("��Ƶ����ʧ��");
            return;
        }
        if (status == 10101)
        {
            Debug.LogError("����Ự�ѽ���");
            return;
        }
        if (status == 10313)
        {
            Debug.LogError("appid����Ϊ��");
            return;
        }
        if (status == 10317)
        {
            Debug.LogError("�汾�Ƿ�");
            return;
        }
        if (status == 11200)
        {
            Debug.LogError("û��Ȩ��");
            return;
        }
        if (status == 11201)
        {
            Debug.LogError("�����س���");
            return;
        }
        if (status == 10160)
        {
            Debug.LogError("�������ݸ�ʽ�Ƿ�");
            return;
        }
        if (status == 10161)
        {
            Debug.LogError("base64����ʧ��");
            return;
        }
        if (status == 10163)
        {
            Debug.LogError("ȱ�ٱش����������߲������Ϸ�������ԭ�����ϸ������");
            return;
        }
        if (status == 10200)
        {
            Debug.LogError("��ȡ���ݳ�ʱ");
            return;
        }
        if (status == 10222)
        {
            Debug.LogError("�����쳣");
            return;
        }
    }


    #endregion

    #region ���ݶ���

    /// <summary>
    /// ���͵�����
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
