using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionCom : MonoBehaviour
{

    [Header("emotion type")]
    public EmotionType emotionType;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

    }

    private void Start()
    {       
        
        StartCoroutine(Play());
    }


    protected IEnumerator Play()
    {   
        yield return new WaitForSeconds( UnityEngine.Random.Range(0.3f,0.8f));

        while (1 == 1)
        {
            string enumName = GetEnumName<EmotionType>((int)emotionType);
         
            animator.Play(enumName,0,0f);
            yield return new WaitForSeconds(1.2f);

        }

    }

    public string GetEnumName<T>(int value)
    {
        string name = "";
        name = Enum.Parse(typeof(T), Enum.GetName(typeof(T), value)).ToString();
        return name;
    }


}
