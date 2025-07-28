// DogCommandController.cs - UDP����ģ��
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

public class UDPCommand : MonoBehaviour
{
    private int dogPort = 43893;
    private string targetIP = "192.168.2.1";
    private UdpClient udpClient;

    // ָ��ṹ��
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CommandHead
    {
        public uint code;
        public uint parameters_size;
        public uint type;
    }

    void Start()
    {
        try
        {
            udpClient = new UdpClient();
            udpClient.Connect(targetIP, dogPort);
            Debug.Log($"UDP ��ʼ�����: {targetIP}:{dogPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP��ʼ������: {e.Message}");
        }
    }

    void Update()
    {
        HeartbeatCommand();
    }

    #region ������ָ���
    public void HeartbeatCommand()
    {
        SendRobotCommand(0x21040001, 0, 0);
    }


    #endregion

    public void SendRobotCommand(uint c, uint p, uint t, params byte[][] parameters)
    {
        CommandHead head = new()
        {
            code = c,  
            parameters_size = p,
            type = t
        };
        // ͨ�Ŵ���
        SendDogCommand(head);

    }

    private void SendDogCommand(CommandHead cmd)
    {
        try
        {
            // ���ṹ��ת��ΪС�����ֽ�����
            byte[] data = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(ConvertToLittleEndian(cmd.code)), 0, data, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(ConvertToLittleEndian(cmd.parameters_size)), 0, data, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(ConvertToLittleEndian(cmd.type)), 0, data, 8, 4);

            // ����UDP���ݰ�
            udpClient.Send(data, data.Length);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Send command failed: {e.Message}");
        }
    }

    // ȷ��С���ֽ���
    private uint ConvertToLittleEndian(uint value)
    {
        if (BitConverter.IsLittleEndian) return value;
        return ReverseBytes(value);
    }

    // �ֽڷ�ת�����ڴ��ϵͳ��
    private uint ReverseBytes(uint value)
    {
        return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
               (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
    }

    void OnDestroy()
    {
        udpClient?.Close();
    }
}