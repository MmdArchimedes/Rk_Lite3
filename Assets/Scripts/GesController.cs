using Rokid.UXR.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GesController : MonoBehaviour
{
    [Header("机器人通信控制器")]
    public UDPCommand commandController;

    [Header("UI 显示")]
    public Text infoText;
    public Text logText;
    public float gestureCooldown = 0.5f;

    private CustomGestureType lastLeft;
    private CustomGestureType lastRight;
    private CustomGestureType currLeft;
    private CustomGestureType currRight;

    private bool isGestureOpen = false;

    #region 手势类型
    // 手势类型枚举
    public enum CustomGestureType
    {
        None,
        ThumbUp,    //打开手势发送指令
        ShutDown_1,
        ShutDown_2,//1,2 组合手势 关闭手势发送指令
        Grip,
        Pinch,
        OneFingerLeft,
        OneFingerRight,
        TwoFingers,
        ThreeFingers,
        FourClosePalm,    
        FourSpreadPalm, //四指 张开 手心 忽略大拇指
        FourCloseBack,
        FourSpreadBack
    }
    // 手势对应动作配置
    public struct GestureConfig
    {
        public string gestureName;        // 手势名称
        public string action;        // 对应动作
        public uint code;
        public uint parameters_size;
        public uint type;
    }
    private static readonly Dictionary<CustomGestureType, GestureConfig> customGestureConfigs =
        new()
    {
    // 手心四指张开 -> 起立
    { CustomGestureType.FourSpreadPalm, new GestureConfig{gestureName = "手心四指张开", action = "起立", code = 0x21010C0A, parameters_size = 1, type = 0} },

    // 手背四指张开 -> 趴下
    { CustomGestureType.FourSpreadBack, new GestureConfig{gestureName = "手背四指张开", action = "趴下", code = 0x21010C0A, parameters_size = 2, type = 0} },

    // 手心握拳 -> 软急停
    { CustomGestureType.Pinch, new GestureConfig{gestureName = "捏合", action = "软急停", code = 0x21020C0E, parameters_size = 0, type = 0} },

    // 手背四指并拢 -> 前进
    { CustomGestureType.FourCloseBack, new GestureConfig{gestureName = "手背四指并拢", action = "前进", code = 0x21010C0A, parameters_size = 3, type = 0} },

    // 手心四指并拢 -> 后退
    { CustomGestureType.FourClosePalm, new GestureConfig{gestureName = "手心四指并拢", action = "后退", code = 0x21010C0A, parameters_size = 4, type = 0} },

    // 手背握拳 -> 停止
    { CustomGestureType.Grip, new GestureConfig{gestureName = "握拳", action = "停止", code = 0x21010C0A, parameters_size = 7, type = 0} },

     // 一指伸直 -> 左转
    { CustomGestureType.OneFingerLeft, new GestureConfig{gestureName = "向左指", action = "左转", code = 0x21010C0A, parameters_size = 13, type = 0} },

    // 一指伸直 -> 右转
    { CustomGestureType.OneFingerRight, new GestureConfig{gestureName = "向右指", action = "右转", code = 0x21010C0A, parameters_size = 14, type = 0} },

    // 二指伸直 -> 打招呼
    { CustomGestureType.TwoFingers, new GestureConfig{gestureName = "二指伸直", action = "打招呼", code = 0x21010C0A, parameters_size = 22, type = 0} },

    // 三指伸直 -> 太空步
    { CustomGestureType.ThreeFingers, new GestureConfig{gestureName = "三指伸直", action = "太空步", code = 0x2101030C, parameters_size = 0, type = 0} },
    };
    #endregion
    void Start()
    {
        // 初始化
        lastLeft = CustomGestureType.None;
        currLeft = CustomGestureType.None;
        currRight = CustomGestureType.None;
        lastRight = CustomGestureType.None;
    }

    private void FixedUpdate()
    {
        currLeft = GestureCheck(HandType.LeftHand);
        currRight = GestureCheck(HandType.RightHand);
        if(currLeft== CustomGestureType.ThumbUp || currRight == CustomGestureType.ThumbUp)
        {
            isGestureOpen = true;
            infoText.text = "手势打开！";
        }            
        if(currLeft== CustomGestureType.ShutDown_1 && currRight == CustomGestureType.ShutDown_2)
        {
            isGestureOpen = false;
            infoText.text = "手势关闭！";
        }
        if (currLeft == CustomGestureType.ShutDown_2 && currRight == CustomGestureType.ShutDown_1)
        {
            isGestureOpen = false;
            infoText.text = "手势关闭！";
        }
        if(isGestureOpen )
        {
            if (currLeft != lastLeft || currRight != lastRight)
            {
                // 检查左手是否有变化并发送指令
                if (currLeft != lastLeft && currLeft != CustomGestureType.None)
                {

                    if (customGestureConfigs.TryGetValue(currLeft, out var leftConfig))
                    {
                        infoText.text = $"手势：{leftConfig.gestureName} 指令：{leftConfig.action}";
                        commandController.SendRobotCommand(leftConfig.code, leftConfig.parameters_size, leftConfig.type);
                    }
                }

                // 检查右手是否有变化并发送指令
                if (currRight != lastRight && currRight != CustomGestureType.None)
                {
                    if (customGestureConfigs.TryGetValue(currRight, out var rightConfig))
                    {
                        infoText.text = $"手势：{rightConfig.gestureName} 指令：{rightConfig.action}";
                        commandController.SendRobotCommand(rightConfig.code, rightConfig.parameters_size, rightConfig.type);
                    }
                }
                // 更新上一次手势状态
                lastLeft = currLeft;
                lastRight = currRight;
            }
        }
       
    }

    private CustomGestureType GestureCheck(HandType hand)
    {
        HandOrientation handOrientation = GesEventInput.Instance.GetHandOrientation(hand);//手心/手背
        bool isSpread = CheckHandOpen(hand); // 检测并拢/张开状态

        //手指伸直
        bool isThumbExtended = CheckFingerExtension("Thumb", hand); //不准确 弃用
        bool isIndexExtended = CheckFingerExtension("Index", hand);
        bool isMiddleExtended = CheckFingerExtension("Middle", hand);
        bool isRingExtended = CheckFingerExtension("Ring", hand);
        bool isPinkyExtended = CheckFingerExtension("Pinky", hand);
        //Rokid基础手势
        GestureType baseGestureType = GesEventInput.Instance.GetGestureType(hand);

        if (baseGestureType == GestureType.Pinch)
        {
            return CustomGestureType.Pinch;
        }

        //四指伸直
        if (isIndexExtended && isMiddleExtended && isRingExtended && isPinkyExtended)
        {
            //手心
            if (handOrientation == HandOrientation.Palm)
            {
                //张开
                if (isSpread)
                    return CustomGestureType.FourSpreadPalm;
                else
                    return CustomGestureType.FourClosePalm;
            }
            //手背
            else if (handOrientation == HandOrientation.Back)
            {
                if (isSpread)
                    return CustomGestureType.FourSpreadBack;
                else
                    return CustomGestureType.FourCloseBack;
            }
            else
            {
                Vector3 palmCenter = GetSkeletonPose(SkeletonIndexFlag.PALM, hand);
                Vector3 wrist = GetSkeletonPose(SkeletonIndexFlag.WRIST, hand);
                Vector3 palmNormal = (palmCenter - wrist).normalized;
                //logText.text = "手掌法线:" + palmNormal.ToString();
                if (palmNormal.y<0.3f)
                    return CustomGestureType.ShutDown_1;
            }   
        }
        else if (isIndexExtended && isMiddleExtended && isRingExtended && !isPinkyExtended)
        {
            return CustomGestureType.ThreeFingers;
        }
        else if (isIndexExtended && isMiddleExtended && !isRingExtended && !isPinkyExtended)
        {
            return CustomGestureType.TwoFingers;
        }
        else if (isIndexExtended && !isMiddleExtended && !isRingExtended && !isPinkyExtended)
        {
            Vector3 indexLog = (GetSkeletonPose(SkeletonIndexFlag.INDEX_FINGER_TIP, hand) - GetSkeletonPose(SkeletonIndexFlag.INDEX_FINGER_MCP, hand)).normalized;
            //logText.text= "食指法线:" + indexLog.ToString();
            if (indexLog.y > 0.5)
                return CustomGestureType.ShutDown_2;
            if (indexLog.x > 0.5)
                return CustomGestureType.OneFingerLeft;
            if (indexLog.x < -0.5)
                return CustomGestureType.OneFingerRight;
        }
        else if (!isIndexExtended && !isMiddleExtended && !isRingExtended && !isPinkyExtended)
        {
            // 获取手掌中心点和手腕位置
            Vector3 palmCenter = GetSkeletonPose(SkeletonIndexFlag.PALM, hand);
            Vector3 wrist = GetSkeletonPose(SkeletonIndexFlag.WRIST, hand);
            Vector3 palmNormal = (palmCenter - wrist).normalized;
            Vector3 thumbVector = (GetSkeletonPose(SkeletonIndexFlag.THUMB_TIP, hand) - GetSkeletonPose(SkeletonIndexFlag.THUMB_MCP, hand)).normalized;
            float thumbAngle = Vector3.Angle(thumbVector, palmNormal);
            if (thumbAngle > 50f)
            {
                Vector3 thumbLog = (GetSkeletonPose(SkeletonIndexFlag.THUMB_TIP, hand) - GetSkeletonPose(SkeletonIndexFlag.THUMB_MCP, hand)).normalized;
                //logText.text = "大拇指法线:" + thumbLog.ToString();
                if (thumbLog.y > 0.7) 
                    return CustomGestureType.ThumbUp;
                if (thumbLog.x > 0.5)
                    return CustomGestureType.OneFingerLeft;
                if (thumbLog.x < -0.5)
                    return CustomGestureType.OneFingerRight;
            }
            if(!isThumbExtended)
                return CustomGestureType.Grip;
        }
        return CustomGestureType.None;
    }

    #region 手势检测方法
    // 手指骨骼节点映射关系
    private static readonly Dictionary<string, SkeletonIndexFlag[]> FINGER_JOINTS = new()
    {
        { "Thumb", new[] { SkeletonIndexFlag.THUMB_MCP, SkeletonIndexFlag.THUMB_IP, SkeletonIndexFlag.THUMB_TIP } },
        { "Index", new[] { SkeletonIndexFlag.INDEX_FINGER_MCP, SkeletonIndexFlag.INDEX_FINGER_PIP, SkeletonIndexFlag.INDEX_FINGER_DIP } },
        { "Middle", new[] { SkeletonIndexFlag.MIDDLE_FINGER_MCP, SkeletonIndexFlag.MIDDLE_FINGER_PIP, SkeletonIndexFlag.MIDDLE_FINGER_DIP } },
        { "Ring", new[] { SkeletonIndexFlag.RING_FINGER_MCP, SkeletonIndexFlag.RING_FINGER_PIP, SkeletonIndexFlag.RING_FINGER_DIP } },
        { "Pinky", new[] { SkeletonIndexFlag.PINKY_MCP, SkeletonIndexFlag.PINKY_PIP, SkeletonIndexFlag.PINKY_DIP} }
    };

    // 通用手指伸直检测方法
    private bool CheckFingerExtension(string fingerName ,HandType handType)
    {
        SkeletonIndexFlag[] jointIndices = FINGER_JOINTS[fingerName];
        // 获取所有关节点位置
        Vector3[] points = new Vector3[jointIndices.Length];
        for (int i = 0; i < jointIndices.Length; i++)
        {
            points[i] = GetSkeletonPose(jointIndices[i], handType);
        }
        // 检测所有关节段是否伸直
        return IsJointExtended(points[0], points[1], points[2]);
    }

    // 三点伸直判断算法
    private bool IsJointExtended(Vector3 A, Vector3 B, Vector3 C)
    {
        Vector3 AB = (B - A).normalized;
        Vector3 BC = (C - B).normalized;
        return Vector3.Dot(AB, BC) > 0.5f; // 点积阈值可调整
    }

    //检测整只手的并拢/张开状态
    private bool CheckHandOpen(HandType hand)
    {
        // 
        Vector3 palmNormal = (GetSkeletonPose(SkeletonIndexFlag.PALM, hand) - GetSkeletonPose(SkeletonIndexFlag.WRIST, hand)).normalized;
        Vector3 thumbVector = (GetSkeletonPose(SkeletonIndexFlag.THUMB_TIP, hand) - GetSkeletonPose(SkeletonIndexFlag.THUMB_MCP, hand)).normalized;
        Vector3 indexVector = (GetSkeletonPose(SkeletonIndexFlag.INDEX_FINGER_TIP, hand) - GetSkeletonPose(SkeletonIndexFlag.INDEX_FINGER_MCP, hand)).normalized;
        //Vector3 middleVector = (GetSkeletonPose(SkeletonIndexFlag.MIDDLE_FINGER_TIP, hand) - GetSkeletonPose(SkeletonIndexFlag.MIDDLE_FINGER_MCP, hand)).normalized;
        Vector3 ringVector = (GetSkeletonPose(SkeletonIndexFlag.RING_FINGER_TIP, hand) - GetSkeletonPose(SkeletonIndexFlag.RING_FINGER_MCP, hand)).normalized;
        Vector3 pinkyVector = (GetSkeletonPose(SkeletonIndexFlag.PINKY_TIP, hand) - GetSkeletonPose(SkeletonIndexFlag.PINKY_MCP, hand)).normalized;
       
        float thumbAngle = Vector3.Angle(thumbVector, palmNormal);
        float indexAngle = Vector3.Angle(indexVector, palmNormal);
        float ringAngle = Vector3.Angle(ringVector, palmNormal);
        float pinkyAngle = Vector3.Angle(pinkyVector, palmNormal);

        // 判断手指伸展比例
        int extendedCount = 0;
        if (thumbAngle > 45) extendedCount++; 
        if (indexAngle > 10) extendedCount++;
        if (ringAngle > 10) extendedCount++;
        if (pinkyAngle > 30) extendedCount++;

        return extendedCount > 2;
    }  

    #endregion

    #region Rokid接口
    private Vector3 GetSkeletonPose(SkeletonIndexFlag index, HandType hand)
    {
        // 调用Rokid SDK获取骨骼点位置
        return GesEventInput.Instance.GetSkeletonPose(index, hand).position;
    }
    #endregion
}