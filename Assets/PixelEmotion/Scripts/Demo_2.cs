using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Demo_2 : MonoBehaviour
{

    public EmotionHUD HUD;

    protected Coroutine coroutine;


    private void Awake()
    {

    }



    public void OnPlayEmotion(int type)
    {

        if (type == -1)
        {

            coroutine = StartCoroutine(LoopEmotionCo());
        }
        else
        {
            if (null != coroutine)
            {
                StopCoroutine(coroutine);
            }

            HUD.PlayEmotion((EmotionType)type);
        }


    }


    public IEnumerator LoopEmotionCo()
    {
        foreach (var val in Enum.GetValues(typeof(EmotionType)))
        {
            HUD.PlayEmotion((EmotionType)val);
            yield return new WaitForSeconds(1.25f);
        }

    }




}
