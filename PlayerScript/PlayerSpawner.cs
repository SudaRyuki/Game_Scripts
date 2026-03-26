using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class PlayerSpawner : MonoBehaviour
{
    void Start()
    {
        if (!PhotonNetwork.InRoom) return;

        string gameMode = (string)PhotonNetwork.CurrentRoom.CustomProperties["gm"];
        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        
        int myTeamID = 1; // デフォルトはTeam 1
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("team"))
        {
            myTeamID = (int)PhotonNetwork.LocalPlayer.CustomProperties["team"];
        }

        // 同じチームの中で、自分が何番目かを計算
        // (同じチームに2人いる場合、出現場所やキャラクターの見た目を分けるため)
        int myIndexInTeam = 0;
        List<int> teamMemberActorNumbers = new List<int>();
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("team") && (int)p.CustomProperties["team"] == myTeamID)
            {
                teamMemberActorNumbers.Add(p.ActorNumber);
            }
        }
        teamMemberActorNumbers.Sort(); // 早く部屋に入った順に並べる
        myIndexInTeam = teamMemberActorNumbers.IndexOf(myActorNumber);
        if (myIndexInTeam < 0) myIndexInTeam = 0;

        string playerPrefabNameToSpawn = ""; 
        Transform spawnPoint = null;

        int teamID = myTeamID;

        // 操作方法：オンライン時は基本WASD。ゲームパッドが刺さっていればそれを使う
        string controlScheme = "Keyboard_WASD";
        if (Gamepad.all.Count > 0) controlScheme = "Gamepad";

        // チームと順番に応じて、プレハブと生成場所を決める
        if (myTeamID == 1)
        {
            if (myIndexInTeam == 0)
            {
                playerPrefabNameToSpawn = GameManager.instance.player1PrefabName;
                spawnPoint = GameManager.instance.team1SpawnPoints[0];
            }
            else // Team1の2人目
            {
                playerPrefabNameToSpawn = GameManager.instance.player3PrefabName;
                spawnPoint = GameManager.instance.team1SpawnPoints.Length > 1 
                    ? GameManager.instance.team1SpawnPoints[1] 
                    : GameManager.instance.team1SpawnPoints[0];
            }
        }
        else // Team 2
        {
            if (myIndexInTeam == 0)
            {
                playerPrefabNameToSpawn = GameManager.instance.player2PrefabName;
                spawnPoint = GameManager.instance.team2SpawnPoints[0];
            }
            else // Team2の2人目
            {
                playerPrefabNameToSpawn = GameManager.instance.player4PrefabName;
                spawnPoint = GameManager.instance.team2SpawnPoints.Length > 1 
                    ? GameManager.instance.team2SpawnPoints[1] 
                    : GameManager.instance.team2SpawnPoints[0];
            }
        }

        if (string.IsNullOrEmpty(playerPrefabNameToSpawn))
        {
            Debug.LogError("生成するプレイヤーPrefabの名前が指定されていません！");
            return;
        }

        //キャラクターを生成
        GameObject playerGO = PhotonNetwork.Instantiate(playerPrefabNameToSpawn, spawnPoint.position, spawnPoint.rotation);
        
        //自分のキャラクターだけに入力設定を行う
        if (playerGO.GetComponent<PhotonView>().IsMine)
        {
            var actions = Instantiate(GameManager.instance.inputActionAsset);
            actions.bindingMask = InputBinding.MaskByGroup(controlScheme);
            
            InputDevice device = null;
            if (controlScheme == "Gamepad")
            {
                device = Gamepad.all[0]; // 各々のPCで1台目のパッドを使う
            }
            else 
            { 
                device = Keyboard.current; 
            }

            int myColorID = (int)PhotonNetwork.LocalPlayer.CustomProperties["color"];

            Player playerScript = playerGO.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.Initialize(actions, device);
                playerScript.teamID = teamID;
                // マテリアルの色を更新するメソッドを呼ぶ
                playerScript.photonView.RPC(nameof(Player.RpcSetColor), RpcTarget.All, myColorID);
            }
            else
            {
                playerGO.GetComponent<Player2>().Initialize(actions, device);
                playerGO.GetComponent<Player2>().teamID = teamID;
                playerGO.GetComponent<PhotonView>().RPC(nameof(Player2.RpcSetColor), RpcTarget.All, myColorID);
            }
        }
    }
}