using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
private Animator myAnimator;
    public enum AnimType
    {
        ScratchHead, // 抓頭
        ShakeHead// 搖頭
    }

    void Start()
    {
        myAnimator = GetComponent<Animator>();
    }

    // 2. 這就是那個「通用的 Function」
    // 外部只要告訴我要播哪一種類型 (type)，剩下的我來煩惱
    public void PlayAction(AnimType type)
    {
        if (myAnimator == null) return;

        switch (type)
        {
            case AnimType.ScratchHead:
                myAnimator.SetTrigger("DoScratch"); // 對應 Animator 裡的 Parameters
                break;

            case AnimType.ShakeHead:
                myAnimator.SetTrigger("DoShake");   // 對應 Animator 裡的 Parameters
                break;
        }
    }
}
