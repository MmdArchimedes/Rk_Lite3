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
    [Header("������ͨ�ſ�����")]
    public UDPCommand commandController;

    [Header("UI ��ʾ")]
    public Text logText;
    public Text lefttext;
    public Text righttext;
    public float gestureCooldown = 0.5f;

    private float lastGestureTime;  // �ϴ����ƴ���ʱ��
    void Start()
    {
        // ��ʼ������
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
    #region ��������
    private void CheckBothHandGestures()
    {
        // �������������
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
                logText.text = ("���ƿ��ƣ����ƣ�����");
                commandController.SendRobotCommand(0x21010C0A, 1, 0);
                break;
            case GestureType.Grip:
                logText.text = ("���ƿ��ƣ���ȭ��ſ��");
                commandController.SendRobotCommand(0x21010C0A, 2, 0);
                break;
            case GestureType.Pinch:
                logText.text = ("���ƿ��ƣ���ϣ�ǰ��");
                commandController.SendRobotCommand(0x21010C0A, 3, 0);
                break;
            case GestureType.OpenPinch:
                logText.text = ("���ƿ��ƣ�����ɿ�������");
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
            log.text = "����";
        else if (hand == HandType.RightHand)
            log.text = "����";
        //����
        if (GesEventInput.Instance.GetHandOrientation(hand) == HandOrientation.Palm)
        {
            log.text += ",����";
            if (left.ExtendedCount == 0)
            {
                if (logText) logText.text = $"����ʶ��: ��ͣ";
                commandController.SendRobotCommand(0x21020C0E, 0, 0);

                log.text += ",��ȭ";
            }
            else if (left.ExtendedCount == 4)
            {
                //��£
                if (!CheckHandOpen(hand))
                {
                    if (logText) logText.text = $"����ʶ��: ǰ��";
                    commandController.SendRobotCommand(0x21010C0A, 3, 0);
                    log.text += $",��£";
                }
                //�ſ�
                else
                {
                    if (logText) logText.text = $"����ʶ��: ����";
                    commandController.SendRobotCommand(0x21010C0A, 4, 0);
                    log.text += $",�ſ�";
                }
            }
            else
            {
                log.text += $",{left.ExtendedCount}��ֱ";
                if (left.ExtendedCount == 1)
                {
                    if (hand == HandType.LeftHand)
                    {
                        if (logText) logText.text = $"����ʶ��: ����ת";
                        commandController.SendRobotCommand(0x21010C0A, 13, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"����ʶ��: ����ת";
                        commandController.SendRobotCommand(0x21010C0A, 14, 0);
                    }
                }
                else if (left.ExtendedCount == 2)
                {
                    if (hand == HandType.LeftHand)
                    {
                        if (logText) logText.text = $"����ʶ��: ̫�ղ�";
                        commandController.SendRobotCommand(0x21010C0A, 1, 0);
                        commandController.SendRobotCommand(0x2101030C, 0, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"����ʶ��: ��շ�";
                        commandController.SendRobotCommand(0x21010C0A, 2, 0);
                        commandController.SendRobotCommand(0x21010502, 0, 0);
                    }
                }
                else 
                {
                    if (hand == HandType.LeftHand)
                    {
                        if (logText) logText.text = $"����ʶ��: Ť���� ";
                        commandController.SendRobotCommand(0x21010C0A, 1, 0);
                        commandController.SendRobotCommand(0x2101020D, 0, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"����ʶ��: ��ǰ�� ";
                        commandController.SendRobotCommand(0x21010C0A, 2, 0);
                        commandController.SendRobotCommand(0x2101050B, 0, 0);
                    }
                }
            }
        }
        //�ֱ�
        else if (GesEventInput.Instance.GetHandOrientation(hand) == HandOrientation.Back)
        {
            log.text += ",�ֱ�";
            //��£
            if (left.ExtendedCount == 0)
            {
                if (logText) logText.text = $"����ʶ��: ֹͣ";
                commandController.SendRobotCommand(0x21010C0A, 7, 0);
                log.text += ",��ȭ";
            }
            else if (left.ExtendedCount == 4)
            {
                if (logText) logText.text = $"����ʶ��: ���к�";
                commandController.SendRobotCommand(0x21010C0A, 22, 0);
                //��£
                if (!CheckHandOpen(hand))
                {
                    log.text += $",��£";
                }
                //�ſ�
                else
                {
                    log.text += $",�ſ�";
                }
            }
            else
            {
                log.text += $",{left.ExtendedCount}��ֱ";
                if( left.ExtendedCount==1)
                {
                    if (hand == HandType.LeftHand) 
                    {
                        if (logText) logText.text = $"����ʶ��: ������";
                        commandController.SendRobotCommand(0x21010C0A, 5, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"����ʶ��: ������";
                        commandController.SendRobotCommand(0x21010C0A, 6, 0);
                    }
                }
                else if (left.ExtendedCount == 2)
                {
                    if (hand == HandType.LeftHand)
                    {
                        if (logText) logText.text = $"����ʶ��: ����";
                        commandController.SendRobotCommand(0x21010C0A, 1, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"����ʶ��: ſ��";
                        commandController.SendRobotCommand(0x21010C0A, 2, 0);
                    }
                }
                else
                {
                    if (hand == HandType.LeftHand)
                    {
                        if (logText) logText.text = $"����ʶ��: Ť����";
                        commandController.SendRobotCommand(0x21010C0A, 1, 0);
                        commandController.SendRobotCommand(0x21010204, 0, 0);
                    }
                    else if (hand == HandType.RightHand)
                    {
                        if (logText) logText.text = $"����ʶ��: ����";
                        commandController.SendRobotCommand(0x21010C0A, 2, 0);
                        commandController.SendRobotCommand(0x21010205, 0, 0);
                    }
                }
            }
        }
    }
    #region ���Ƽ�ⷽ��
    // ��ָ�����ڵ�ӳ���ϵ
    private static readonly Dictionary<string, SkeletonIndexFlag[]> FINGER_JOINTS = new Dictionary<string, SkeletonIndexFlag[]>
    {
        { "Thumb", new[] { SkeletonIndexFlag.THUMB_MCP, SkeletonIndexFlag.THUMB_IP, SkeletonIndexFlag.THUMB_TIP } },
        { "Index", new[] { SkeletonIndexFlag.INDEX_FINGER_MCP, SkeletonIndexFlag.INDEX_FINGER_PIP, SkeletonIndexFlag.INDEX_FINGER_DIP } },
        { "Middle", new[] { SkeletonIndexFlag.MIDDLE_FINGER_MCP, SkeletonIndexFlag.MIDDLE_FINGER_PIP, SkeletonIndexFlag.MIDDLE_FINGER_DIP } },
        { "Ring", new[] { SkeletonIndexFlag.RING_FINGER_MCP, SkeletonIndexFlag.RING_FINGER_PIP, SkeletonIndexFlag.RING_FINGER_DIP } },
        { "Pinky", new[] { SkeletonIndexFlag.PINKY_MCP, SkeletonIndexFlag.PINKY_PIP, SkeletonIndexFlag.PINKY_DIP} }
    };

    // ���ķ����������ֻ�ֵ���ֱ״̬
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

    // ͨ����ָ��ֱ��ⷽ��
    private bool CheckFingerExtension(SkeletonIndexFlag[] jointIndices, HandType handType)
    {
        // ��ȡ���йؽڵ�λ��
        Vector3[] points = new Vector3[jointIndices.Length];
        for (int i = 0; i < jointIndices.Length; i++)
        {
            points[i] = GetSkeletonPose(jointIndices[i], handType).position;
        }
        // ������йؽڶ��Ƿ���ֱ
        if (!IsJointExtended(points[0], points[1], points[2])) 
            return false;
        return true;
    }

    // ������ֱ�ж��㷨
    private bool IsJointExtended(Vector3 A, Vector3 B, Vector3 C)
    {
        Vector3 AB = (B - A).normalized;
        Vector3 BC = (C - B).normalized;
        return Vector3.Dot(AB, BC) > 0.5f; // �����ֵ�ɵ���
    }
    //�����ֻ�ֵĲ�£/�ſ�״̬
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
    // ����洢�ṹ
    public struct FingerExtensionResult
    {
        public bool indexExtended;
        public bool middleExtended;
        public bool ringExtended;
        public bool pinkyExtended;

        // ������������ȡ������ֱ��ָ������
        public int ExtendedCount =>(indexExtended ? 1 : 0) +
                                   (middleExtended ? 1 : 0) +
                                   (ringExtended ? 1 : 0) +
                                   (pinkyExtended ? 1 : 0);

        // ��ʽ�����
        public override string ToString() =>
            $"ʳָ: {indexExtended}, ��ָ: {middleExtended}, ����ָ: {ringExtended}, СĴָ: {pinkyExtended}";
    }
    #endregion

    #region Rokid�ӿ�ģ��
    private Pose GetSkeletonPose(SkeletonIndexFlag index, HandType hand)
    {
        // ����Rokid SDK��ȡ������λ��
        return GesEventInput.Instance.GetSkeletonPose(index, hand);
    }
    #endregion


}