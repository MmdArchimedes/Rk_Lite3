// TouchPadController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>按键→机器人轴向指令的映射</summary>
[System.Serializable]
public struct AxisKeyCmd
{
    public KeyCode key;     // 监听的输入键
    public uint cmdCode; // 指令码
    public int value;   // 速度 / 参数
    public string label;   // UI 显示
}

public class TouchPadController : MonoBehaviour
{
    /* ----------- 发送频率 ----------- */
    private const float SEND_INTERVAL = 0.04f; // 25 Hz

    /* ----------- 速度常量 ----------- */
    // 速度常量（全部高于死区，并留出可加速空间）
    private const int FORWARD_SPEED = 20000;   // 前进
    private const int BACKWARD_SPEED = -20000;  // 后退
    private const int LEFT_TURN_SPEED = -20000;  // 左转
    private const int RIGHT_TURN_SPEED = 20000;   // 右转

    /* ----------- 依赖组件 ----------- */
    [Header("机器人通信控制器")]
    public UDPCommand commandController;   // Inspector 中拖入已有 UDPCommand 组件

    /* ----------- UI ----------- */
    [Header("UI 显示")]
    public Text infoText;

    /* ----------- 按键指令表 ----------- */
    private readonly AxisKeyCmd[] axisCmds = {
        new() { key = KeyCode.LeftArrow,cmdCode = 0x21010135, value = LEFT_TURN_SPEED,label = "TouchPad控制: 左转" },
        new() { key = KeyCode.RightArrow,cmdCode = 0x21010135, value = RIGHT_TURN_SPEED,label = "TouchPad控制: 右转" },
        new() { key = KeyCode.DownArrow,cmdCode = 0x21010130, value = BACKWARD_SPEED,label = "TouchPad控制: 后退" },
        new() { key = KeyCode.UpArrow,cmdCode = 0x21010130, value = FORWARD_SPEED,label = "TouchPad控制: 前进" },
    };

    /* ----------- 内部状态 ----------- */
    private Dictionary<KeyCode, float> nextSendTimes;  // 按键下次允许发送时间

    /* ----------- 初始化 ----------- */

    private void Start()
    {
        nextSendTimes = new Dictionary<KeyCode, float>();
        foreach (var c in axisCmds) nextSendTimes[c.key] = 0f;
    }

    /* ----------- 主循环 ----------- */
    private void Update()
    {
        // 遍历表，统一处理所有按键
        foreach (var cmd in axisCmds)
        {
            float nextTime = nextSendTimes[cmd.key];
            // —— KeyDown：先发一次 & 记录节拍 ——
            if (Input.GetKeyDown(cmd.key))
            {
                SendAxisCommand(cmd.cmdCode, cmd.value);
                nextSendTimes[cmd.key] = Time.time + SEND_INTERVAL;
                if (infoText) infoText.text = cmd.label;
            }

            // —— Key 长按：按 25 Hz 重发 ——
            if (Input.GetKey(cmd.key) && Time.time >= nextTime)
            {
                SendAxisCommand(cmd.cmdCode, cmd.value);
                nextSendTimes[cmd.key] = Time.time + SEND_INTERVAL;
            }

            // —— KeyUp：发送停止(0) ——
            if (Input.GetKeyUp(cmd.key))
            {
                SendAxisCommand(cmd.cmdCode, 0);
                nextSendTimes[cmd.key] = 0f;
            }
        }
    }
    /* ----------- 发送封装 ----------- */
    /// <summary>把 int 速度值作为 p 参数直接发给 UDPCommand</summary>
    private void SendAxisCommand(uint code, int value)
    {
        // UDPCommand 参数: (code, p, t) —— 其中 p 我们直接塞速度数值, t 固定 0
        commandController.SendRobotCommand(code, (uint)value, 0);
    }

    IEnumerator DelayStop()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("2秒后执行这里");
        commandController.SendRobotCommand(0x21010C0A, 7, 0);
    }
}
