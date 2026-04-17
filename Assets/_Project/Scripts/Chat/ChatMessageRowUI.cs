using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChatMessageRowUI : MonoBehaviour
{
    public TMP_Text messageText;

    public void Bind(string text)
    {
        if(messageText)
            messageText.text = text;
    }
}
