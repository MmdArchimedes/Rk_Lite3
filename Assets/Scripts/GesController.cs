using Rokid.UXR.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GesController : MonoBehaviour
{
    [Header("������ͨ�ſ�����")]
    public UDPCommand commandController;

    [Header("UI ��ʾ")]
    public Text infoText;
    public Text logText;
    public float gestureCooldown = 0.5f;

    private CustomGestureType lastLeft;
    private CustomGestureType lastRight;
    private CustomGestureType currLeft;
    private CustomGestureType currRight;

    private bool isGestureOpen = false;

    #region ��������
    // ��������ö��
    public enum CustomGestureType
    {
        None,
        ThumbUp,    //�����Ʒ���ָ��
        ShutDown_1,
        ShutDown_2,//1,2 ������� �ر����Ʒ���ָ��
        Grip,
        Pinch,
        OneFingerLeft,
        OneFingerRight,
        TwoFingers,
        ThreeFingers,
        FourClosePalm,    
        FourSpreadPalm, //��ָ �ſ� ���� ���Դ�Ĵָ
        FourCloseBack,
        FourSpreadBack
    }
    // ���ƶ�Ӧ��������
    public struct GestureConfig
    {
        public string gestureName;        // ��������
        public string action;        // ��Ӧ����
        public uint code;
        public uint parameters_size;
        public uint type;
    }
    private static readonly Dictionary<CustomGestureType, GestureConfig> customGestureConfigs =
        new()
    {
    // ������ָ�ſ� -> ����
    { CustomGestureType.FourSpreadPalm, new GestureConfig{gestureName = "������ָ�ſ�", action = "����", code = 0x21010C0A, parameters_size = 1, type = 0} },

    // �ֱ���ָ�ſ� -> ſ��
    { CustomGestureType.FourSpreadBack, new GestureConfig{gestureName = "�ֱ���ָ�ſ�", action = "ſ��", code = 0x21010C0A, parameters_size = 2, type = 0} },

    // ������ȭ -> ��ͣ
    { CustomGestureType.Pinch, new GestureConfig{gestureName = "���", action = "��ͣ", code = 0x21020C0E, parameters_size = 0, type = 0} },

    // �ֱ���ָ��£ -> ǰ��
    { CustomGestureType.FourCloseBack, new GestureConfig{gestureName = "�ֱ���ָ��£", action = "ǰ��", code = 0x21010C0A, parameters_size = 3, type = 0} },

    // ������ָ��£ -> ����
    { CustomGestureType.FourClosePalm, new GestureConfig{gestureName = "������ָ��£", action = "����", code = 0x21010C0A, parameters_size = 4, type = 0} },

    // �ֱ���ȭ -> ֹͣ
    { CustomGestureType.Grip, new GestureConfig{gestureName = "��ȭ", action = "ֹͣ", code = 0x21010C0A, parameters_size = 7, type = 0} },

     // һָ��ֱ -> ��ת
    { CustomGestureType.OneFingerLeft, new GestureConfig{gestureName = "����ָ", action = "��ת", code = 0x21010C0A, parameters_size = 13, type = 0} },

    // һָ��ֱ -> ��ת
    { CustomGestureType.OneFingerRight, new GestureConfig{gestureName = "����ָ", action = "��ת", code = 0x21010C0A, parameters_size = 14, type = 0} },

    // ��ָ��ֱ -> ���к�
    { CustomGestureType.TwoFingers, new GestureConfig{gestureName = "��ָ��ֱ", action = "���к�", code = 0x21010C0A, parameters_size = 22, type = 0} },

    // ��ָ��ֱ -> ̫�ղ�
    { CustomGestureType.ThreeFingers, new GestureConfig{gestureName = "��ָ��ֱ", action = "̫�ղ�", code = 0x2101030C, parameters_size = 0, type = 0} },
    };
    #endregion
    void Start()
    {
        // ��ʼ��
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
            infoText.text = "���ƴ򿪣�";
        }            
        if(currLeft== CustomGestureType.ShutDown_1 && currRight == CustomGestureType.ShutDown_2)
        {
            isGestureOpen = false;
            infoText.text = "���ƹرգ�";
        }
        if (currLeft == CustomGestureType.ShutDown_2 && currRight == CustomGestureType.ShutDown_1)
        {
            isGestureOpen = false;
            infoText.text = "���ƹرգ�";
        }
        if(isGestureOpen )
        {
            if (currLeft != lastLeft || currRight != lastRight)
            {
                // ��������Ƿ��б仯������ָ��
                if (currLeft != lastLeft && currLeft != CustomGestureType.None)
                {

                    if (customGestureConfigs.TryGetValue(currLeft, out var leftConfig))
                    {
                        infoText.text = $"���ƣ�{leftConfig.gestureName} ָ�{leftConfig.action}";
                        commandController.SendRobotCommand(leftConfig.code, leftConfig.parameters_size, leftConfig.type);
                    }
                }

                // ��������Ƿ��б仯������ָ��
                if (currRight != lastRight && currRight != CustomGestureType.None)
                {
                    if (customGestureConfigs.TryGetValue(currRight, out var rightConfig))
                    {
                        infoText.text = $"���ƣ�{rightConfig.gestureName} ָ�{rightConfig.action}";
                        commandController.SendRobotCommand(rightConfig.code, rightConfig.parameters_size, rightConfig.type);
                    }
                }
                // ������һ������״̬
                lastLeft = currLeft;
                lastRight = currRight;
            }
        }
       
    }

    private CustomGestureType GestureCheck(HandType hand)
    {
        HandOrientation handOrientation = GesEventInput.Instance.GetHandOrientation(hand);//����/�ֱ�
        bool isSpread = CheckHandOpen(hand); // ��Ⲣ£/�ſ�״̬

        //��ָ��ֱ
        bool isThumbExtended = CheckFingerExtension("Thumb", hand); //��׼ȷ ����
        bool isIndexExtended = CheckFingerExtension("Index", hand);
        bool isMiddleExtended = CheckFingerExtension("Middle", hand);
        bool isRingExtended = CheckFingerExtension("Ring", hand);
        bool isPinkyExtended = CheckFingerExtension("Pinky", hand);
        //Rokid��������
        GestureType baseGestureType = GesEventInput.Instance.GetGestureType(hand);

        if (baseGestureType == GestureType.Pinch)
        {
            return CustomGestureType.Pinch;
        }

        //��ָ��ֱ
        if (isIndexExtended && isMiddleExtended && isRingExtended && isPinkyExtended)
        {
            //����
            if (handOrientation == HandOrientation.Palm)
            {
                //�ſ�
                if (isSpread)
                    return CustomGestureType.FourSpreadPalm;
                else
                    return CustomGestureType.FourClosePalm;
            }
            //�ֱ�
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
                //logText.text = "���Ʒ���:" + palmNormal.ToString();
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
            //logText.text= "ʳָ����:" + indexLog.ToString();
            if (indexLog.y > 0.5)
                return CustomGestureType.ShutDown_2;
            if (indexLog.x > 0.5)
                return CustomGestureType.OneFingerLeft;
            if (indexLog.x < -0.5)
                return CustomGestureType.OneFingerRight;
        }
        else if (!isIndexExtended && !isMiddleExtended && !isRingExtended && !isPinkyExtended)
        {
            // ��ȡ�������ĵ������λ��
            Vector3 palmCenter = GetSkeletonPose(SkeletonIndexFlag.PALM, hand);
            Vector3 wrist = GetSkeletonPose(SkeletonIndexFlag.WRIST, hand);
            Vector3 palmNormal = (palmCenter - wrist).normalized;
            Vector3 thumbVector = (GetSkeletonPose(SkeletonIndexFlag.THUMB_TIP, hand) - GetSkeletonPose(SkeletonIndexFlag.THUMB_MCP, hand)).normalized;
            float thumbAngle = Vector3.Angle(thumbVector, palmNormal);
            if (thumbAngle > 50f)
            {
                Vector3 thumbLog = (GetSkeletonPose(SkeletonIndexFlag.THUMB_TIP, hand) - GetSkeletonPose(SkeletonIndexFlag.THUMB_MCP, hand)).normalized;
                //logText.text = "��Ĵָ����:" + thumbLog.ToString();
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

    #region ���Ƽ�ⷽ��
    // ��ָ�����ڵ�ӳ���ϵ
    private static readonly Dictionary<string, SkeletonIndexFlag[]> FINGER_JOINTS = new()
    {
        { "Thumb", new[] { SkeletonIndexFlag.THUMB_MCP, SkeletonIndexFlag.THUMB_IP, SkeletonIndexFlag.THUMB_TIP } },
        { "Index", new[] { SkeletonIndexFlag.INDEX_FINGER_MCP, SkeletonIndexFlag.INDEX_FINGER_PIP, SkeletonIndexFlag.INDEX_FINGER_DIP } },
        { "Middle", new[] { SkeletonIndexFlag.MIDDLE_FINGER_MCP, SkeletonIndexFlag.MIDDLE_FINGER_PIP, SkeletonIndexFlag.MIDDLE_FINGER_DIP } },
        { "Ring", new[] { SkeletonIndexFlag.RING_FINGER_MCP, SkeletonIndexFlag.RING_FINGER_PIP, SkeletonIndexFlag.RING_FINGER_DIP } },
        { "Pinky", new[] { SkeletonIndexFlag.PINKY_MCP, SkeletonIndexFlag.PINKY_PIP, SkeletonIndexFlag.PINKY_DIP} }
    };

    // ͨ����ָ��ֱ��ⷽ��
    private bool CheckFingerExtension(string fingerName ,HandType handType)
    {
        SkeletonIndexFlag[] jointIndices = FINGER_JOINTS[fingerName];
        // ��ȡ���йؽڵ�λ��
        Vector3[] points = new Vector3[jointIndices.Length];
        for (int i = 0; i < jointIndices.Length; i++)
        {
            points[i] = GetSkeletonPose(jointIndices[i], handType);
        }
        // ������йؽڶ��Ƿ���ֱ
        return IsJointExtended(points[0], points[1], points[2]);
    }

    // ������ֱ�ж��㷨
    private bool IsJointExtended(Vector3 A, Vector3 B, Vector3 C)
    {
        Vector3 AB = (B - A).normalized;
        Vector3 BC = (C - B).normalized;
        return Vector3.Dot(AB, BC) > 0.5f; // �����ֵ�ɵ���
    }

    //�����ֻ�ֵĲ�£/�ſ�״̬
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

        // �ж���ָ��չ����
        int extendedCount = 0;
        if (thumbAngle > 45) extendedCount++; 
        if (indexAngle > 10) extendedCount++;
        if (ringAngle > 10) extendedCount++;
        if (pinkyAngle > 30) extendedCount++;

        return extendedCount > 2;
    }  

    #endregion

    #region Rokid�ӿ�
    private Vector3 GetSkeletonPose(SkeletonIndexFlag index, HandType hand)
    {
        // ����Rokid SDK��ȡ������λ��
        return GesEventInput.Instance.GetSkeletonPose(index, hand).position;
    }
    #endregion
}