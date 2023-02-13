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
            cur.voteMeBtn.gameObject.SetActive(false);
        }

        //check vote
        int totalVote = 0;

        int maxVote = 0;
        Photon.Realtime.Player voted = null;

        foreach (var (key, value) in VotingManager.Instance.spyVotes)
        {
            totalVote += value;
            if (value > maxVote)
            {
                maxVote = value;
                voted = key;
            }
        }

        //reveal spy
        if (totalVote == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            foreach (PlayerController cur in VotingManager.Instance.allPlayers)
            {
                cur.PV.RPC("revealPhrase", RpcTarget.AllBuffered);
                StartCoroutine(nameof(delayClearSpy));
            }

            PV.RPC(nameof(message), RpcTarget.AllBuffered, "You voted" + voted.NickName, false);
        }
    }

    IEnumerator delayClearSpy()
    {
        yield return new WaitForSeconds(2.0f);

        //ask all players to clear
        foreach (PlayerController cur in VotingManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(clearList), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void clearList()
    {
        foreach (Transform transform in votingList)
        {
            Destroy(transform.gameObject);
        }
    } 

    [PunRPC]
    void castVoteSpy(Photon.Realtime.Player votedPlayer, string voter)
    {
        VotingManager.Instance.spyVotes[votedPlayer]++;

        GameObject cur = Instantiate(votingItem, votingList);
        cur.GetComponent<TextMeshProUGUI>().text = voter;
    }

    [PunRPC]
    void message(string text, bool countDown)
    {
        VotingManager.Instance.messageText.text = text;

        if (VotingManager.Instance.curCoroutine != null)
        {
            VotingManager.Instance.StopCoroutine(VotingManager.Instance.curCoroutine);
        }

        if (countDown)
        {
            VotingManager.Instance.curCoroutine = VotingManager.Instance.StartCoroutine("CountDown");
        }
    }
}

