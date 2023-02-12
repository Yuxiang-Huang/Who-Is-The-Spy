using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Vote : MonoBehaviour
{
    public PhotonView PV;
    public Photon.Realtime.Player player;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    public void voteMe()
    {
        PV.RPC(nameof(castVoteSpy), RpcTarget.AllBuffered, player);
    }

    [PunRPC]
    public void castVoteSpy(Photon.Realtime.Player votedPlayer)
    {
        VotingManager.Instance.spyVotes[votedPlayer]++;
        string str = "";

        foreach (var (key, value) in VotingManager.Instance.spyVotes)
        {
            str += key.NickName + " : " + value + ", ";
        }

        VotingManager.Instance.messageText.text = str;
    }
}
