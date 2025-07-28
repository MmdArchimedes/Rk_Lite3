using Rokid.UXR.Module;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#region 数据结构
/// <summary>
/// 一条语音→机器人指令的映射
/// </summary>
[System.Serializable]
public struct RobotVoiceCmd
{
    public string phrase;   // 中文短语（用户说的话）
    public string pinyin;   // 对应拼音（Rokid 注册时用）
    public uint cmdCode;  // 机器人通信 CommandCode
    public uint parameter;// 参数
    public uint cmdType;  // 指令类型
}
#endregion

public class Voice : MonoBehaviour
{
    private bool isInit = false;

    [Header("机器人通信控制器")]
    public UDPCommand commandController;

    [Header("UI 显示")]
    public Text InfoText;

    private readonly RobotVoiceCmd[] cmds = {
        //1
        new() { phrase = "起立",   pinyin = "qi li",cmdCode = 0x21010C0A, parameter = 1,  cmdType = 0 },
        //2
        new() { phrase = "坐下",   pinyin = "zuo xia", cmdCode = 0x21010C0A, parameter = 2,  cmdType = 0 },
        //3
        new() { phrase = "前进",   pinyin = "qian jing", cmdCode = 0x21010C0A, parameter = 3,  cmdType = 0 },
        //4
        new() { phrase = "后退",   pinyin = "hou tui", cmdCode = 0x21010C0A, parameter = 4,  cmdType = 0 },
        //5
        new() { phrase = "向左走",   pinyin = "xiang zuo zou",  cmdCode = 0x21010C0A, parameter = 5,  cmdType = 0 },
        //6
        new() { phrase = "向右走",   pinyin = "xaing you zou", cmdCode = 0x21010C0A, parameter = 6,  cmdType = 0 },
        //7 第一种方法是语音指令中对应的停止，对不属于0x21010C0A系列的指令无效，第二种方法对所有指令生效但会回到趴下的初始状态
        new() { phrase = "停止",   pinyin = "ting zhi",     cmdCode = 0x21010C0A, parameter = 7,  cmdType = 0 },     
        //8
        new() { phrase = "向左转", pinyin = "xiang zuo zhuan",cmdCode = 0x21010C0A, parameter = 13, cmdType = 0 },
        //9
        new() { phrase = "向右转", pinyin = "xiang you zhuan",cmdCode = 0x21010C0A, parameter = 14, cmdType = 0 },
        //0
        new() { phrase = "打招呼", pinyin = "da zhao hu",   cmdCode = 0x21010C0A, parameter = 22, cmdType = 0 },
        //new() { phrase = "打招呼", pinyin = "da zhao hu",   cmdCode = 0x21010507, parameter = 22, cmdType = 0 },
        //-
        new() { phrase = "后空翻", pinyin = "hou kong fan", cmdCode = 0x21010502, parameter = 0,  cmdType = 0 },
        //z
        new() { phrase = "扭身体", pinyin = "niu shen ti",cmdCode = 0x21010204, parameter = 0,  cmdType = 0 },
        //x
        new() { phrase = "向前跳", pinyin = "xiang qian tiao", cmdCode = 0x2101050B, parameter = 0,  cmdType = 0 },
        //c
        new() { phrase = "扭身跳", pinyin = "niu shen tiao", cmdCode = 0x2101020D, parameter = 0,  cmdType = 0 },
        //v
        new() { phrase = "太空步", pinyin = "tai kong bu", cmdCode = 0x2101030C, parameter = 0,  cmdType = 0 },
        //b
        new() { phrase = "翻身", pinyin = "fan shen", cmdCode = 0x21010205, parameter = 0,  cmdType = 0 },
        //n
        new() { phrase = "结束表演",   pinyin = "jie shu biao yan", cmdCode = 0x21020C0E, parameter = 0,  cmdType = 0 },
        //m
        new() { phrase = "匍匐",   pinyin = "pu fu", cmdCode = 0x21010406, parameter = 0,  cmdType = 0 },
    };

    // phrase → RobotVoiceCmd 的快速查表
    private Dictionary<string, RobotVoiceCmd> lookup;


    #region Unity 生命周期
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
            Debug.LogError("-RKX- 没有 RECORD_AUDIO 权限，返回主场景");
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
        Debug.Log("-RKX- Voice 模块销毁，清理指令");
        OfflineVoiceModule.Instance.ClearAllInstruct();
        OfflineVoiceModule.Instance.Commit();
    }
    #endregion

    #region 初始化与注册
    private void InitializeVoiceControl()
    {
        if (isInit) return;

        Debug.Log("-RKX- 初始化 Rokid 语音模块");
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
    
    #region 语音回调
    /// <summary>
    /// 被 OfflineVoiceModule 调用的回调
    /// </summary>
    public void OnVoiceCommand(string phrase)
    {
        if (!lookup.TryGetValue(phrase, out var cmd))
        {
            if (InfoText) InfoText.text = $"未知指令: {phrase}";
            return;
        }

        if (InfoText) InfoText.text = $"语音识别: {phrase}";
        //Debug.Log($"发送机器人指令: {phrase} → 0x{cmd.cmdCode:X8}, {cmd.parameter}, {cmd.cmdType}");       
        commandController.SendRobotCommand(cmd.cmdCode, cmd.parameter, cmd.cmdType);
    }
    public void OnGestureCommand(string phrase)
    {
        lookup.TryGetValue(phrase, out var cmd);
        if (InfoText) InfoText.text = $"手势识别: {phrase}";
        //Debug.Log($"发送机器人指令: {phrase} → 0x{cmd.cmdCode:X8}, {cmd.parameter}, {cmd.cmdType}");       
        commandController.SendRobotCommand(cmd.cmdCode, cmd.parameter, cmd.cmdType);
    }

    #endregion

    #region 本地键盘测试
    // 对应 cmds 顺序的测试按键
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
