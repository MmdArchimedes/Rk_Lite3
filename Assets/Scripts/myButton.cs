using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.UI;
public class MyButton : MonoBehaviour
{
    public UDPCommand commandController;   // Inspector 中拖入已有 UDPCommand 组件
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
                Debug.Log("关闭UI");
            }
            else
            {
                canvas.SetActive(true);
                Debug.Log("打开UI");
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
            infoText.text = ($"移动模式");
        }
            
        if (code == "stand")
        {
            commandController.SendRobotCommand(0x21010D05, 0, 0);
            infoText.text = ($"原地模式");
        }
        if (code == "close")
        {
            if (screen.activeSelf)
            {
                screen.SetActive(false);
                infoText.text = ($"关闭视频流");
            }
                
            else
            {
                screen.SetActive(true);
                infoText.text = ($"打开视频流");
            }
                
            
        }
        if (code == "gesture")
        {
            if (gesture.activeSelf)
            {
                gesture.SetActive(false);
                infoText.text = ($"关闭手势");
            }
            else
            {
                gesture.SetActive(true);
                infoText.text = ($"打开手势");
            }
        }
        if (code == "voice")
        {
            if (voice.activeSelf)
            {
                voice.SetActive(false);
                infoText.text = ($"关闭语音");
            }
            else
            {
                voice.SetActive(true);
                infoText.text = ($"打开语音");
            }
        }
    }
}
