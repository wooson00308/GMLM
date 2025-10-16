using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionHUD : MonoBehaviour
{


    private Animator animator;

    protected SpriteRenderer spriteRenderer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.color = new Color(255, 255, 255, 0);
    }


    //play emotion
    public void PlayEmotion(EmotionType emotionType)
    {
        
        StartCoroutine(PlayEmotionCO(emotionType));
    }

    protected IEnumerator PlayEmotionCO(EmotionType emotionType)
    {

        spriteRenderer.color = new Color(255, 255, 255, 255);

        string enumName = GetEnumName<EmotionType>((int)emotionType);

        animator.Play(enumName, 0, 0f);
        yield return new WaitForSeconds(0.9f);

        spriteRenderer.color = new Color(255, 255, 255, 0);

    }

    public string GetEnumName<T>(int value)
    {
        string name = "";
        name = Enum.Parse(typeof(T), Enum.GetName(typeof(T), value)).ToString();
        return name;
    }


}
