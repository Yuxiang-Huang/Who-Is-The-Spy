using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Vote : MonoBehaviour
{
    public PhotonView PV;
    public Photon.Realtime.Player player;

    public void voteMe()
    {
        PV.RPC(nameof(castVote), RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void castVote()
    {
        VotingManager.Instance.spyVotes[player]++;
        string str = "";

        foreach (var (key, value) in VotingManager.Instance.spyVotes)
        {
            str += value + " : ";
        }

        VotingManager.Instance.messageText.text = str;
    }
}
