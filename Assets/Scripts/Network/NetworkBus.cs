using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class NetworkBus : NetworkBehaviour {

  public TextMesh myMesh;
  GameObject myObj;
  Transform myTrans;

  [SyncVar] public string username;

  void Start()
  {
    myObj = gameObject;
    myTrans = transform;

    if (isLocalPlayer)
    {
      Destroy(myMesh.gameObject);

      myObj.name = "A local player";
      username = Settings.username;

      GameController.player = gameObject;
      GameController.OnNetworkInitialized();

      if (Network.isServer)
      {
        // If a client, going to have to wait to receive the seed from the server
        GameController.OnSeedReceived("");
      }
    }
    else
    {
      myMesh.text = username;
      myObj.name = "A player named "+username;
    }
  }

  void OnSeedReceived(NetworkMessage msg)
  {
    Debug.LogError("seed received");
  }

  public void Initialize()
  {
    
  }

  // === Network Commands ===
  // Digging/building
  [Command]
  public void CmdTryPlaceBlock(Vector3 point, BlockType heldBlock)
  {
    if ((point-myTrans.position).sqrMagnitude > GameController.playerActions.digDistSquared)
      return;

    int[] chunkCoord = Util.GetChunkCoord(point);
    int[] blockCoord = Util.GetBlockCoord(point, chunkCoord);

    bool success = GameController.chunkController.CanPlaceBlock(heldBlock, chunkCoord, blockCoord);

    if (success)
      RpcReceiveBlockPlaced(chunkCoord, blockCoord, heldBlock);
  }
  [ClientRpc]
  void RpcReceiveBlockPlaced(int[] chunkCoord, int[] blockCoord, BlockType type)
  {
    GameController.chunkController.ReceiveBlockPlaced(chunkCoord, blockCoord, type);
  }

  [Command]
  public void CmdTryDigBlock(Vector3 point)
  {
    if ((point-myTrans.position).sqrMagnitude > GameController.playerActions.digDistSquared)
      return;

    int[] chunkCoord = Util.GetChunkCoord(point);
    int[] blockCoord = Util.GetBlockCoord(point, chunkCoord);

    BlockType b = GameController.chunkController.CanDigBlock(chunkCoord, blockCoord);
    if (Block.attributes[(int)b].isDiggable)
      RpcReceiveBlockDug(chunkCoord, blockCoord);
  }
  [ClientRpc]
  void RpcReceiveBlockDug(int[] chunkCoord, int[] blockCoord)
  {
    bool success = GameController.chunkController.ReceiveBlockDug(chunkCoord, blockCoord);
    GameController.playerActions.OnBlockDug(chunkCoord, blockCoord);
  }


  // Chat
  [Command]
  public void CmdSendChat(string username, string chat)
  {
    RpcReceiveChat(username + ": " + chat);
  }
  [ClientRpc]
  void RpcReceiveChat(string msg)
  {
    ChatUI.ReceiveChat(msg);
  }

  // Login/config
  [Command]
  public void CmdSetUsername(string user)
  {
    username = user;

    RpcUpdateUsername(username);
  }
  [ClientRpc]
  void RpcUpdateUsername(string user)
  {
    if (!(myMesh==null))
      myMesh.text = username;

    myObj.name = "A player named "+username;

    ChatUI.SystemMessage(username+" has joined the game.");
  }

  [Command]
  public void CmdAskForSeed()
  {

  }
  // === /Network Commands ===


  void OnChatMessage(NetworkMessage msg)
  {
    MyMsg message = msg.ReadMessage<MyMsg>();
    ChatUI.ReceiveChat(message.data);
  }

  public class MyMsg : MessageBase
  {
    public string data;

    public MyMsg(){}
    public MyMsg(string d)
    {
      data = d;
    }
  }
}

class MyMsgType : MsgType{
  public const int UpdateSeed = 6921;
}
