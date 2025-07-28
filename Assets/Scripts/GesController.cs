using Rokid.UXR.Interaction;
using Rokid.UXR.Module;
using Rokid.UXR.Native;
using Rokid.UXR.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
public class GesController : MonoBehaviour
{
    [Header("机器人通信控制器")]
    public UDPCommand commandController;

    [Header("UI 显示")]
    public Text logText;
    public Text lefttext;
    public Text righttext;
    public float gestureCooldown = 0.5f;

    private float lastGestureTime;  // 上次手势触发时间
    void Start()
    {
        // 初始化变量
        lastGestureTime = Time.time;
    }

    void Update()
    {

    }

    private void FixedUpdate()
    {
        if (Time.time - lastGestureTime > gestureCooldown)
        {
            CheckBothHandGestures_2();
            lastGestureTime = Time.time; 
        }
    }
    #region 基础手势
    private void CheckBothHandGestures()
    {
        // 检测左右手手势
        GestureType left = GesEventInput.Instance.GetGestureType(HandType.LeftHand);
        GestureType right = GesEventInput.Instance.GetGestureType(HandType.RightHand);

        HandleGesture(left);
        HandleGesture(right);
    }
    private void HandleGesture(GestureType gesture)
    {
        switch (gesture)
        {
            case GestureType.Palm:
                logText.text = ("手势控制：手掌，起立");
                commandController.SendRobotCommand(0x21010C0A, 1, 0);
                break;
            case GestureType.Grip:
                logText.text = ("手势控制：握拳，趴下");
                commandController.SendRobotCommand(0x21010C0A, 2, 0);
                break;
            case GestureType.Pinch:
                logText.text = ("手势控制：捏合，前进");
                commandController.SendRobotCommand(0x21010C0A, 3, 0);
                break;
            case GestureType.OpenPinch:
                logText.text = ("手势控制：捏合松开，后退");
                commandController.SendRobotCommand(0x21010C0A, 4, 0);
                break;
        }
    }
    #endregion
    
    private void CheckBothHandGestures_2()
    {
        gesturecheck(HandType.LeftHand, lefttext);
        gesturecheck(HandType.RightHand, righttext);
    }
    private void gesturecheck(HandType hand,Text log)
    {
        FingerExtensionResult left = CheckHandFingers(hand);
        if(hand== HandType.LeftHand) 
            log.text = "左手";
        else if (hand == HandType.RightHand)
            log.text = "右手";
        //手心
        if (GesEventInput.Instance.GetHandOrientation(hand) == HandOrientation.Palm)
        {
            log.text += ",手心";
            if (left.ExtendedCount == 0)
            {
                if (logText) logText.text = $"手势识别: 软急停";
                commandController.SendRobotCommand(0x21020C0E, 0, 0);

                log.text += ",握拳";
            }
            else if (left.ExtendedCount == 4)
            {
                //并拢
                if (!CheckHandOpen(hand))
                {
                    if (logText) logText.text = $"手势识别: 前进";
                    commandController.SendRobotCommand(0x21010C0A, 3, 0);
                    log.text += $",并拢";
                }
                //张开
                else
                {
                    if (logText) logText.text = $"手势识别: 后退";
                    commandController.SendRobotCommand(0x21010C0A, 4, 0);
                    log.text += $",张开";
                }
            }
            else
            {
                log.text += $",{left.ExtendedCount}伸直";
                if (left.ExtendedCount == 1)
                {
                    if (hand == HandType.LeftHand)
                    {
                        if (logText) logText.text = $"手势识别: 向左转";
                        commandController.SendRobotCommand(0x21010C0A, 13, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"手势识别: 向右转";
                        commandController.SendRobotCommand(0x21010C0A, 14, 0);
                    }
                }
                else if (left.ExtendedCount == 2)
                {
                    if (hand == HandType.LeftHand)
                    {
                        if (logText) logText.text = $"手势识别: 太空步";
                        commandController.SendRobotCommand(0x21010C0A, 1, 0);
                        commandController.SendRobotCommand(0x2101030C, 0, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"手势识别: 后空翻";
                        commandController.SendRobotCommand(0x21010C0A, 2, 0);
                        commandController.SendRobotCommand(0x21010502, 0, 0);
                    }
                }
                else 
                {
                    if (hand == HandType.LeftHand)
                    {
                        if (logText) logText.text = $"手势识别: 扭身跳 ";
                        commandController.SendRobotCommand(0x21010C0A, 1, 0);
                        commandController.SendRobotCommand(0x2101020D, 0, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"手势识别: 向前跳 ";
                        commandController.SendRobotCommand(0x21010C0A, 2, 0);
                        commandController.SendRobotCommand(0x2101050B, 0, 0);
                    }
                }
            }
        }
        //手背
        else if (GesEventInput.Instance.GetHandOrientation(hand) == HandOrientation.Back)
        {
            log.text += ",手背";
            //并拢
            if (left.ExtendedCount == 0)
            {
                if (logText) logText.text = $"手势识别: 停止";
                commandController.SendRobotCommand(0x21010C0A, 7, 0);
                log.text += ",握拳";
            }
            else if (left.ExtendedCount == 4)
            {
                if (logText) logText.text = $"手势识别: 打招呼";
                commandController.SendRobotCommand(0x21010C0A, 22, 0);
                //并拢
                if (!CheckHandOpen(hand))
                {
                    log.text += $",并拢";
                }
                //张开
                else
                {
                    log.text += $",张开";
                }
            }
            else
            {
                log.text += $",{left.ExtendedCount}伸直";
                if( left.ExtendedCount==1)
                {
                    if (hand == HandType.LeftHand) 
                    {
                        if (logText) logText.text = $"手势识别: 向左走";
                        commandController.SendRobotCommand(0x21010C0A, 5, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"手势识别: 向右走";
                        commandController.SendRobotCommand(0x21010C0A, 6, 0);
                    }
                }
                else if (left.ExtendedCount == 2)
                {
                    if (hand == HandType.LeftHand)
                    {
                        if (logText) logText.text = $"手势识别: 起立";
                        commandController.SendRobotCommand(0x21010C0A, 1, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"手势识别: 趴下";
                        commandController.SendRobotCommand(0x21010C0A, 2, 0);
                    }
                }
                else
                {
                    if (hand == HandType.LeftHand)
                    {
                        if (logText) logText.text = $"手势识别: 扭身体";
                        commandController.SendRobotCommand(0x21010C0A, 1, 0);
                        commandController.SendRobotCommand(0x21010204, 0, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"手势识别: 翻身";
                        commandController.SendRobotCommand(0x21010C0A, 2, 0);
                        commandController.SendRobotCommand(0x21010205, 0, 0);
                    }
                }
            }
        }
    }
    #region 手势检测方法
    // 手指骨骼节点映射关系
    private static readonly Dictionary<string, SkeletonIndexFlag[]> FINGER_JOINTS = new Dictionary<string, SkeletonIndexFlag[]>
    {
        { "Thumb", new[] { SkeletonIndexFlag.THUMB_MCP, SkeletonIndexFlag.THUMB_IP, SkeletonIndexFlag.THUMB_TIP } },
        { "Index", new[] { SkeletonIndexFlag.INDEX_FINGER_MCP, SkeletonIndexFlag.INDEX_FINGER_PIP, SkeletonIndexFlag.INDEX_FINGER_DIP } },
        { "Middle", new[] { SkeletonIndexFlag.MIDDLE_FINGER_MCP, SkeletonIndexFlag.MIDDLE_FINGER_PIP, SkeletonIndexFlag.MIDDLE_FINGER_DIP } },
        { "Ring", new[] { SkeletonIndexFlag.RING_FINGER_MCP, SkeletonIndexFlag.RING_FINGER_PIP, SkeletonIndexFlag.RING_FINGER_DIP } },
        { "Pinky", new[] { SkeletonIndexFlag.PINKY_MCP, SkeletonIndexFlag.PINKY_PIP, SkeletonIndexFlag.PINKY_DIP} }
    };

    // 核心方法：检测整只手的伸直状态
    public FingerExtensionResult CheckHandFingers(HandType handType)
    {
        return new FingerExtensionResult
        {
            indexExtended = CheckFingerExtension(FINGER_JOINTS["Index"], handType),
            middleExtended = CheckFingerExtension(FINGER_JOINTS["Middle"], handType),
            ringExtended = CheckFingerExtension(FINGER_JOINTS["Ring"], handType),
            pinkyExtended = CheckFingerExtension(FINGER_JOINTS["Pinky"], handType)
        };
    }

    // 通用手指伸直检测方法
    private bool CheckFingerExtension(SkeletonIndexFlag[] jointIndices, HandType handType)
    {
        // 获取所有关节点位置
        Vector3[] points = new Vector3[jointIndices.Length];
        for (int i = 0; i < jointIndices.Length; i++)
        {
            points[i] = GetSkeletonPose(jointIndices[i], handType).position;
        }
        // 检测所有关节段是否伸直
        if (!IsJointExtended(points[0], points[1], points[2])) 
            return false;
        return true;
    }

    // 三点伸直判断算法
    private bool IsJointExtended(Vector3 A, Vector3 B, Vector3 C)
    {
        Vector3 AB = (B - A).normalized;
        Vector3 BC = (C - B).normalized;
        return Vector3.Dot(AB, BC) > 0.5f; // 点积阈值可调整
    }
    //检测整只手的并拢/张开状态
    public bool CheckHandOpen(HandType hand)
    {
        Vector3[] angels=new Vector3[5];
        angels[0] = (GetSkeletonPose(SkeletonIndexFlag.THUMB_IP, hand).position - GetSkeletonPose(SkeletonIndexFlag.THUMB_MCP, hand).position).normalized;
        angels[1] = (GetSkeletonPose(SkeletonIndexFlag.INDEX_FINGER_PIP, hand).position - GetSkeletonPose(SkeletonIndexFlag.INDEX_FINGER_MCP, hand).position).normalized;
        angels[2] = (GetSkeletonPose(SkeletonIndexFlag.MIDDLE_FINGER_PIP, hand).position - GetSkeletonPose(SkeletonIndexFlag.MIDDLE_FINGER_MCP, hand).position).normalized;
        angels[3] = (GetSkeletonPose(SkeletonIndexFlag.RING_FINGER_PIP, hand).position - GetSkeletonPose(SkeletonIndexFlag.RING_FINGER_MCP, hand).position).normalized;
        angels[4] = (GetSkeletonPose(SkeletonIndexFlag.PINKY_PIP, hand).position - GetSkeletonPose(SkeletonIndexFlag.PINKY_MCP, hand).position).normalized;

        for (int i = 0; i < 4; i++)
        {
            if (Vector3.Dot(angels[i], angels[i + 1]) > 0.98f)
            {
                return false;
            }          
        }
        return true;
    }
    // 结果存储结构
    public struct FingerExtensionResult
    {
        public bool indexExtended;
        public bool middleExtended;
        public bool ringExtended;
        public bool pinkyExtended;

        // 辅助方法：获取所有伸直手指的数量
        public int ExtendedCount =>(indexExtended ? 1 : 0) +
                                   (middleExtended ? 1 : 0) +
                                   (ringExtended ? 1 : 0) +
                                   (pinkyExtended ? 1 : 0);

        // 格式化输出
        public override string ToString() =>
            $"食指: {indexExtended}, 中指: {middleExtended}, 无名指: {ringExtended}, 小拇指: {pinkyExtended}";
    }
    #endregion

    #region Rokid接口模拟
    private Pose GetSkeletonPose(SkeletonIndexFlag index, HandType hand)
    {
        // 调用Rokid SDK获取骨骼点位置
        return GesEventInput.Instance.GetSkeletonPose(index, hand);
    }
    #endregion


}