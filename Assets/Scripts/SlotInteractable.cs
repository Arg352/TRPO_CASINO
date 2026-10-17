using System.Collections;
using UnityEngine;

public class SlotInteractable : MonoBehaviour
{
    public enum ButtonAction
    {
        NormalSpin,  
        IncreaseBet,
        DecreaseBet,
        SetMaxBet,
        ResetBet,
        CheatSymbol0,
        CheatSymbol1,
        CheatSymbol2,
        CheatSymbol3,
        CheatSymbol4
    }

    [Header("Настройки аудио")]
    [Tooltip("Компонент AudioSource на этой кнопке или общем объекте")]
    public AudioSource audioSource;
    [Tooltip("Звук клика по кнопке")]
    public AudioClip clickSound;

    [Header("Настройки кнопки")]
    public LeverAnimator slotMachine;
    public ButtonAction action = ButtonAction.IncreaseBet;

    [Header("Настройки 3D Анимации")]
    [Tooltip("На сколько смещать кнопку при нажатии. Для 3D моделей значения должны быть маленькими!")]
    public Vector3 pressOffset = new Vector3(0f, -0.05f, 0f);
    public float pressDuration = 0.05f;
    public float releaseDuration = 0.08f;

    private Vector3 startLocalPosition;
    private Coroutine activeCoroutine;
    private bool isInitialized = false;

    void Awake()
    {
        if (slotMachine == null)
        {
            slotMachine = GetComponentInParent<LeverAnimator>();
            if (slotMachine == null && transform.root != null)
            {
                slotMachine = transform.root.GetComponentInChildren<LeverAnimator>();
            }
        }
    }

    void Start()
    {
        startLocalPosition = transform.localPosition;
        isInitialized = true;
    }

    public void Interact()
    {
        if (slotMachine == null)
        {
            Debug.LogWarning($"На кнопке {gameObject.name} не найден LeverAnimator!");
            return;
        }

        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        if (action != ButtonAction.NormalSpin)
        {
            AnimatePress();
        }

        switch (action)
        {
            case ButtonAction.NormalSpin:
                slotMachine.TriggerSpin();
                break;
            case ButtonAction.IncreaseBet:
                slotMachine.IncreaseBet();
                break;
            case ButtonAction.DecreaseBet:
                slotMachine.DecreaseBet();
                break;
            case ButtonAction.SetMaxBet:
                slotMachine.SetMaxBet();
                break;
            case ButtonAction.ResetBet:
                slotMachine.ResetBet();
                break;
            case ButtonAction.CheatSymbol0:
                slotMachine.TriggerCheatSpin(0);
                break;
            case ButtonAction.CheatSymbol1:
                slotMachine.TriggerCheatSpin(1);
                break;
            case ButtonAction.CheatSymbol2:
                slotMachine.TriggerCheatSpin(2);
                break;
            case ButtonAction.CheatSymbol3:
                slotMachine.TriggerCheatSpin(3);
                break;
            case ButtonAction.CheatSymbol4:
                slotMachine.TriggerCheatSpin(4);
                break;
        }
    }

    private void AnimatePress()
    {
        if (!gameObject.activeInHierarchy || !isInitialized) return;

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        activeCoroutine = StartCoroutine(ButtonPressRoutine());
    }

    private IEnumerator ButtonPressRoutine()
    {
        Vector3 targetPos = startLocalPosition + pressOffset;

        float timer = 0f;
        while (timer < pressDuration)
        {
            timer += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, timer / pressDuration);
            yield return null;
        }
        transform.localPosition = targetPos;

        yield return new WaitForSeconds(0.02f);

        timer = 0f;
        while (timer < releaseDuration)
        {
            timer += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(transform.localPosition, startLocalPosition, timer / releaseDuration);
            yield return null;
        }
        transform.localPosition = startLocalPosition;

        activeCoroutine = null;
    }
}
