using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.UI;
public class MyButton : MonoBehaviour
{
    public UDPCommand commandController;   // Inspector ���������� UDPCommand ���
    public Text infoText;
    GameObject screen;
    GameObject gesture;
    GameObject voice;
    GameObject canvas;
    // Start is called before the first frame update
    void Start()
    {
        screen = GameObject.Find("screen");
        gesture = GameObject.Find("Gesture");
        voice = GameObject.Find("Voice");
        canvas = GameObject.Find("Canvas");

        if (screen != null) screen.SetActive(true);
        if (gesture != null) gesture.SetActive(true);
        if (voice != null) voice.SetActive(false);
        if (canvas != null) canvas.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Joystick1Button3))
        {
            if (canvas.activeSelf)
            {
                canvas.SetActive(false);
                Debug.Log("�ر�UI");
            }
            else
            {
                canvas.SetActive(true);
                Debug.Log("��UI");
            }
        }
    }
    public void Click(string code)
    {
        
        if (code == "updown")
        {
            commandController.SendRobotCommand(0x21010202, 0, 0);
            
        }
            
        if (code == "move")
        {
            commandController.SendRobotCommand(0x21010D06, 0, 0);
            infoText.text = ($"�ƶ�ģʽ");
        }
            
        if (code == "stand")
        {
            commandController.SendRobotCommand(0x21010D05, 0, 0);
            infoText.text = ($"ԭ��ģʽ");
        }
        if (code == "close")
        {
            if (screen.activeSelf)
            {
                screen.SetActive(false);
                infoText.text = ($"�ر���Ƶ��");
            }
                
            else
            {
                screen.SetActive(true);
                infoText.text = ($"����Ƶ��");
            }
                
            
        }
        if (code == "gesture")
        {
            if (gesture.activeSelf)
            {
                gesture.SetActive(false);
                infoText.text = ($"�ر�����");
            }
            else
            {
                gesture.SetActive(true);
                infoText.text = ($"������");
            }
        }
        if (code == "voice")
        {
            if (voice.activeSelf)
            {
                voice.SetActive(false);
                infoText.text = ($"�ر�����");
            }
            else
            {
                voice.SetActive(true);
                infoText.text = ($"������");
            }
        }
    }
}
