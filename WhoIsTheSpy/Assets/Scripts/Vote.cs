using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using TMPro;

public class Vote : MonoBehaviour
{
    public PhotonView PV;
    public Photon.Realtime.Player player;
    [SerializeField] Transform votingList;
    [SerializeField] GameObject votingItem;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    public void voteMe()
    {
        PV.RPC(nameof(castVoteSpy), RpcTarget.AllBuffered, player, PhotonNetwork.NickName);

        //clear buttons
        foreach (PlayerController cur in VotingManager.Instance.allPlayers)
        {
            cur.votingBtn.gameObject.SetActive(false);
        }
    }

    [PunRPC]
    public void castVoteSpy(Photon.Realtime.Player votedPlayer, string voter)
    {
        VotingManager.Instance.spyVotes[votedPlayer]++;

        GameObject cur = Instantiate(votingItem, votingList);
        cur.GetComponent<TextMeshProUGUI>().text = voter;

        //string str = "";

        //foreach (var (key, value) in VotingManager.Instance.spyVotes)
        //{
        //    str += key.NickName + " : " + value + ", ";
        //}

        //VotingManager.Instance.messageText.text = str;
    }


}
