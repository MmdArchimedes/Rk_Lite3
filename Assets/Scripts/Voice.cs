using Rokid.UXR.Module;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#region ���ݽṹ
/// <summary>
/// һ��������������ָ���ӳ��
/// </summary>
[System.Serializable]
public struct RobotVoiceCmd
{
    public string phrase;   // ���Ķ���û�˵�Ļ���
    public string pinyin;   // ��Ӧƴ����Rokid ע��ʱ�ã�
    public uint cmdCode;  // ������ͨ�� CommandCode
    public uint parameter;// ����
    public uint cmdType;  // ָ������
}
#endregion

public class Voice : MonoBehaviour
{
    private bool isInit = false;

    [Header("������ͨ�ſ�����")]
    public UDPCommand commandController;

    [Header("UI ��ʾ")]
    public Text InfoText;

    private readonly RobotVoiceCmd[] cmds = {
        //1
        new() { phrase = "����",   pinyin = "qi li",cmdCode = 0x21010C0A, parameter = 1,  cmdType = 0 },
        //2
        new() { phrase = "����",   pinyin = "zuo xia", cmdCode = 0x21010C0A, parameter = 2,  cmdType = 0 },
        //3
        new() { phrase = "ǰ��",   pinyin = "qian jing", cmdCode = 0x21010C0A, parameter = 3,  cmdType = 0 },
        //4
        new() { phrase = "����",   pinyin = "hou tui", cmdCode = 0x21010C0A, parameter = 4,  cmdType = 0 },
        //5
        new() { phrase = "������",   pinyin = "xiang zuo zou",  cmdCode = 0x21010C0A, parameter = 5,  cmdType = 0 },
        //6
        new() { phrase = "������",   pinyin = "xaing you zou", cmdCode = 0x21010C0A, parameter = 6,  cmdType = 0 },
        //7 ��һ�ַ���������ָ���ж�Ӧ��ֹͣ���Բ�����0x21010C0Aϵ�е�ָ����Ч���ڶ��ַ���������ָ����Ч����ص�ſ�µĳ�ʼ״̬
        new() { phrase = "ֹͣ",   pinyin = "ting zhi",     cmdCode = 0x21010C0A, parameter = 7,  cmdType = 0 },     
        //8
        new() { phrase = "����ת", pinyin = "xiang zuo zhuan",cmdCode = 0x21010C0A, parameter = 13, cmdType = 0 },
        //9
        new() { phrase = "����ת", pinyin = "xiang you zhuan",cmdCode = 0x21010C0A, parameter = 14, cmdType = 0 },
        //0
        new() { phrase = "���к�", pinyin = "da zhao hu",   cmdCode = 0x21010C0A, parameter = 22, cmdType = 0 },
        //new() { phrase = "���к�", pinyin = "da zhao hu",   cmdCode = 0x21010507, parameter = 22, cmdType = 0 },
        //-
        new() { phrase = "��շ�", pinyin = "hou kong fan", cmdCode = 0x21010502, parameter = 0,  cmdType = 0 },
        //z
        new() { phrase = "Ť����", pinyin = "niu shen ti",cmdCode = 0x21010204, parameter = 0,  cmdType = 0 },
        //x
        new() { phrase = "��ǰ��", pinyin = "xiang qian tiao", cmdCode = 0x2101050B, parameter = 0,  cmdType = 0 },
        //c
        new() { phrase = "Ť����", pinyin = "niu shen tiao", cmdCode = 0x2101020D, parameter = 0,  cmdType = 0 },
        //v
        new() { phrase = "̫�ղ�", pinyin = "tai kong bu", cmdCode = 0x2101030C, parameter = 0,  cmdType = 0 },
        //b
        new() { phrase = "����", pinyin = "fan shen", cmdCode = 0x21010205, parameter = 0,  cmdType = 0 },
        //n
        new() { phrase = "��������",   pinyin = "jie shu biao yan", cmdCode = 0x21020C0E, parameter = 0,  cmdType = 0 },
        //m
        new() { phrase = "����",   pinyin = "pu fu", cmdCode = 0x21010406, parameter = 0,  cmdType = 0 },
    };

    // phrase �� RobotVoiceCmd �Ŀ��ٲ��
    private Dictionary<string, RobotVoiceCmd> lookup;


    #region Unity ��������
    private void Awake()
    {
        if (!Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
        {
            Permission.RequestUserPermission("android.permission.RECORD_AUDIO");
        }
    }

    private void Start()
    {
        if (!Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
        {
            Debug.LogError("-RKX- û�� RECORD_AUDIO Ȩ�ޣ�����������");
        }
        InitializeVoiceControl();
    }

    private void Update()
    {
        //if (TestMode) RunVoiceTest();
    }

    private void OnDestroy()
    {
        if (!gameObject.scene.isLoaded) return;
        Debug.Log("-RKX- Voice ģ�����٣�����ָ��");
        OfflineVoiceModule.Instance.ClearAllInstruct();
        OfflineVoiceModule.Instance.Commit();
    }
    #endregion

    #region ��ʼ����ע��
    private void InitializeVoiceControl()
    {
        if (isInit) return;

        Debug.Log("-RKX- ��ʼ�� Rokid ����ģ��");
        ModuleManager.Instance.RegistModule("com.rokid.voicecommand.VoiceCommandHelper", false);
        OfflineVoiceModule.Instance.ChangeVoiceCommandLanguage(LANGUAGE.CHINESE);

        lookup = new Dictionary<string, RobotVoiceCmd>();

        foreach (var c in cmds)
        {
            OfflineVoiceModule.Instance.AddInstruct(
                LANGUAGE.CHINESE,
                c.phrase,
                c.pinyin,
                gameObject.name,
                nameof(OnVoiceCommand));
            lookup[c.phrase] = c;
        }

        OfflineVoiceModule.Instance.Commit();
        isInit = true;
    }
    #endregion
    
    #region �����ص�
    /// <summary>
    /// �� OfflineVoiceModule ���õĻص�
    /// </summary>
    public void OnVoiceCommand(string phrase)
    {
        if (!lookup.TryGetValue(phrase, out var cmd))
        {
            if (InfoText) InfoText.text = $"δָ֪��: {phrase}";
            return;
        }

        if (InfoText) InfoText.text = $"����ʶ��: {phrase}";
        //Debug.Log($"���ͻ�����ָ��: {phrase} �� 0x{cmd.cmdCode:X8}, {cmd.parameter}, {cmd.cmdType}");       
        commandController.SendRobotCommand(cmd.cmdCode, cmd.parameter, cmd.cmdType);
    }
    public void OnGestureCommand(string phrase)
    {
        lookup.TryGetValue(phrase, out var cmd);
        if (InfoText) InfoText.text = $"����ʶ��: {phrase}";
        //Debug.Log($"���ͻ�����ָ��: {phrase} �� 0x{cmd.cmdCode:X8}, {cmd.parameter}, {cmd.cmdType}");       
        commandController.SendRobotCommand(cmd.cmdCode, cmd.parameter, cmd.cmdType);
    }

    #endregion

    #region ���ؼ��̲���
    // ��Ӧ cmds ˳��Ĳ��԰���
    private readonly KeyCode[] testKeys = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        KeyCode.Minus, KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M       
    };

    private void RunVoiceTest()
    {
        int max = Mathf.Min(cmds.Length, testKeys.Length);
        for (int i = 0; i < max; i++)
        {
            if (Input.GetKeyDown(testKeys[i]))
            {
                OnVoiceCommand(cmds[i].phrase);
            }
        }
    }
    #endregion
}
