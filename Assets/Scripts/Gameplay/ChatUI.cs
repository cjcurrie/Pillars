using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class ChatUI : MonoBehaviour
{
  public float scale = 1;

  static List<string> messages;

  static string chatEntry = "";
  int maxChatEntryLength = 42;
  Vector2 scrollPosition = Vector2.zero;
  int lineHeight;
  bool chatOpen = true;
  //static ChatUI instance;

  // Rect cache
  Rect chatWindowRect, chatViewRect, chatEntryRect;

	public void Initialize()
  {
    messages = new List<string>();
    SystemMessage("Welcome to Pillars.");
    SystemMessage("To get started, type '/login [username]'");
    SystemMessage("To host, type '/host [optional id]'");
    SystemMessage("To join, type '/join' [optional id]");
    //instance = this;

    InitializeRects();

    InputController.RegisterKeyDownEvent(ChatKeys.HideChat, ToggleChat);
  }

  void InitializeRects()
  {
    lineHeight = (int)(22);
    int windowHeight=(int)(200*scale), windowWidth=(int)(400*scale);

    chatEntryRect = new Rect(20*scale, Screen.height-lineHeight-20*scale, windowWidth, lineHeight+10*scale);
    chatWindowRect = new Rect(chatEntryRect.x, chatEntryRect.y-windowHeight, windowWidth, windowHeight);
    chatViewRect = new Rect(10*scale,10*scale,chatWindowRect.width-20*scale, windowHeight*3);

    scrollPosition = new Vector2(0,chatViewRect.height);
  }
	
	public void OnMyGUI()
  {
    GUI.skin.label.wordWrap = true;
    string focus = GUI.GetNameOfFocusedControl();

    if (Event.current.type == EventType.KeyDown)
    {
        // no matter where, but if Escape was pushed, close the dialog
        if (Event.current.keyCode == KeyCode.Escape)
        {
          Settings.SetCursor(true);
          GUI.FocusControl("Default");
          chatOpen = false;
        }

        if (Event.current.keyCode == KeyCode.Return)
        {
          if (focus == "ChatBox")
          {
            if (chatEntry != "")
            {
              ParseMessage(chatEntry);
              chatEntry = "";
            }
            Settings.SetCursor(true);
            GUI.FocusControl("Default");
          }
          else
          {
            Settings.SetCursor(false);
            chatOpen = true;
            GUI.FocusControl("ChatBox");
          }
        }
    }

    if (chatOpen)
    {
      GUI.SetNextControlName("Default");
      GUI.Box(chatWindowRect, "");

      scrollPosition = GUI.BeginScrollView(chatWindowRect, scrollPosition, chatViewRect, false, true);
        for(int i=0;i<messages.Count;i++)
        {
          GUI.Label(new Rect(20*scale, chatViewRect.height - (i+1)*lineHeight, chatViewRect.width, lineHeight), messages[i]);
        }
      GUI.EndScrollView();

      GUI.SetNextControlName("ChatBox");
      chatEntry = GUI.TextField(chatEntryRect, chatEntry, maxChatEntryLength);
    }
  }

  void ParseMessage(string msg)
  {
    if (msg == "")
      return;

    // A command was entered
    if (msg.IndexOf("/") == -1)
    {
      //ChatUI.ReceiveChat("You: "+msg);
      GameController.networkBus.CmdSendChat(Settings.username, msg);
    }
    else
    {
      string command = "", parameter = "";

      int spaceIndex = msg.IndexOf(" ");
      if (spaceIndex != -1)
      {
        command = msg.Substring(0,spaceIndex);
        parameter = msg.Substring(spaceIndex+1);
      }
      else
      {
        command = msg;
      }

      //Debug.Log("command: '"+command+"' parameter: '"+parameter+"'");

      switch (command)
      {
        case "/login":
          if (parameter == "")
            SystemMessage("You must provide a username e.g. '/login Colin'");
          else
          {
            SystemMessage("Logged in as "+parameter);
            Settings.username = parameter;
          }
        break;
        case "/host":
          if (Settings.username == "")
            SystemMessage("You must first log in using '/login [username]'");
          else if (parameter == "")
            NetworkController.HostP2PServer("default");
          else
            NetworkController.HostP2PServer(parameter);
        break;

        case "/join":
          if (Settings.username == "")
            SystemMessage("You must first log in using '/login [username]'");
          else if (parameter == "")
            NetworkController.JoinP2PServer("default");
          else
            NetworkController.JoinP2PServer(parameter);
        break;

        default:
          SystemMessage("Command '/"+command+"'' not recognized");
        break;
      }
    }
  }

  void ToggleChat()
  {
    chatOpen = !chatOpen;
  }

  public static void ReceiveChat(string chat)
  {
    messages.Insert(0,chat);
  }

  public static void SystemMessage(string msg)
  {
    messages.Insert(0,"<SYSTEM>"+msg);
  }
}
