using System.Collections;
using UnityEngine;
using TMPro;

public class LeverAnimator : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip spinSound;
    public AudioClip winFanfareSound;
    public AudioClip leverSound;

    [Header("Reels Settings")]
    public Transform[] reels;
    public float spinSpeed = 1200f;
    public float spinDuration = 1.0f;
    public float snapAngle = 72f;
    public float offsetAngle = 0f;

    [Header("UI & Economy")]
    public TMP_Text balanceText;
    public TMP_Text winText;
    public TMP_Text betText;
    public int balance = 1000;
    public int bet = 10;
    public int betStep = 10;

    private bool isPlaying = false;
    private Vector3[] initialRotations;
    private bool forceWin = false;
    private int forcedSymbol = 0;

    void Start()
    {
        initialRotations = new Vector3[reels.Length];
        for (int i = 0; i < reels.Length; i++)
        {
            initialRotations[i] = reels[i].localEulerAngles;
        }

        UpdateUI();
        if (winText != null) winText.text = "";

        if (audioSource != null)
        {
            audioSource.loop = true;
        }
    }

    public void IncreaseBet()
    {
        if (isPlaying) return;

        if (bet + betStep <= balance)
        {
            bet += betStep;
            UpdateUI();
        }
    }

    public void DecreaseBet()
    {
        if (isPlaying) return;

        if (bet - betStep >= betStep)
        {
            bet -= betStep;
            UpdateUI();
        }
    }

    public void SetMaxBet()
    {
        if (isPlaying) return;

        if (balance >= betStep)
        {
            bet = (balance / betStep) * betStep;
            UpdateUI();
        }
    }

    public void ResetBet()
    {
        if (isPlaying) return;

        bet = betStep;
        UpdateUI();
    }

    public void TriggerSpin()
    {
        if (CanPlay())
        {
            StartCoroutine(PullAndSpin());
        }
    }

    public void TriggerCheatSpin(int symbol)
    {
        if (!CanPlay()) return;

        forceWin = true;
        forcedSymbol = symbol;
        StartCoroutine(PullAndSpin());
    }

    private bool CanPlay()
    {
        if (isPlaying) return false;

        if (balance < bet || bet <= 0)
        {
            if (winText != null) winText.text = "ERR!";
            return false;
        }

        return true;
    }

    private IEnumerator PullAndSpin()
    {
        isPlaying = true;
        balance -= bet;
        if (winText != null) winText.text = "";
        UpdateUI();

        Quaternion startRot = transform.localRotation;
        Quaternion downRot = startRot * Quaternion.Euler(0f, 0f, 45f);
        if (audioSource != null && leverSound != null)
        {
            audioSource.PlayOneShot(leverSound);
        }

        float leverTimer = 0f;
        float downDuration = 0.15f;
        while (leverTimer < downDuration)
        {
            leverTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, leverTimer / downDuration);
            transform.localRotation = Quaternion.Slerp(startRot, downRot, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);

        leverTimer = 0f;
        float upDuration = 0.25f;
        while (leverTimer < upDuration)
        {
            leverTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, leverTimer / upDuration);
            transform.localRotation = Quaternion.Slerp(downRot, startRot, t);
            yield return null;
        }
        transform.localRotation = startRot;

        if (audioSource != null && spinSound != null)
        {
            audioSource.clip = spinSound;
            audioSource.Play();
        }

        int[] resultSymbols = GenerateSpinResults();
        int winAmount = CalculatePayout(resultSymbols);

        Coroutine[] spinCoroutines = new Coroutine[reels.Length];
        for (int i = 0; i < reels.Length; i++)
        {
            float extraSpinTime = i * 0.5f;
            spinCoroutines[i] = StartCoroutine(SpinReelRoutine(i, resultSymbols[i], spinDuration + extraSpinTime));
        }

        for (int i = 0; i < reels.Length; i++)
        {
            yield return spinCoroutines[i];
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (winAmount > 0)
        {
            balance += winAmount;
            if (winText != null) winText.text = "WIN: " + winAmount.ToString("D4");
            if (audioSource != null && winFanfareSound != null)
            {
                audioSource.PlayOneShot(winFanfareSound);
            }
        }

        

        if (bet > balance && balance >= betStep)
        {
            bet = (balance / betStep) * betStep;
        }

        UpdateUI();
        isPlaying = false;
    }

    private int[] GenerateSpinResults()
    {
        int[] results = new int[3];

        if (forceWin)
        {
            results[0] = forcedSymbol;
            results[1] = forcedSymbol;
            results[2] = forcedSymbol;
            forceWin = false;
            return results;
        }

        int roll = Random.Range(0, 10000);
        int winSymbol = -1;

        if (roll < 5) winSymbol = 2;
        else if (roll < 55) winSymbol = 3;
        else if (roll < 205) winSymbol = 4;
        else if (roll < 705) winSymbol = 0;
        else if (roll < 2705) winSymbol = 1;

        if (winSymbol != -1)
        {
            results[0] = winSymbol;
            results[1] = winSymbol;
            results[2] = winSymbol;
        }
        else
        {
            results[0] = Random.Range(0, 5);
            results[1] = Random.Range(0, 5);
            results[2] = Random.Range(0, 5);

            if (results[0] == results[1] && results[1] == results[2])
            {
                results[2] = (results[2] + 1) % 5;
            }
        }

        return results;
    }

    private int CalculatePayout(int[] symbols)
    {
        if (symbols[0] == symbols[1] && symbols[1] == symbols[2])
        {
            switch (symbols[0])
            {
                case 2: return bet * 50;
                case 3: return bet * 15;
                case 4: return bet * 10;
                case 0: return bet * 5;
                case 1: return bet * 2;
            }
        }
        return 0;
    }

    private IEnumerator SpinReelRoutine(int index, int targetSymbol, float fastSpinTime)
    {
        Transform reel = reels[index];
        float timer = 0f;

        while (timer < fastSpinTime)
        {
            reel.Rotate(spinSpeed * Time.deltaTime, 0, 0, Space.Self);
            timer += Time.deltaTime;
            yield return null;
        }

        float startAngleX = reel.localEulerAngles.x;
        float targetAngleX = (initialRotations[index].x + (targetSymbol * snapAngle) + offsetAngle) % 360f;

        if (targetAngleX <= startAngleX)
        {
            targetAngleX += 360f;
        }

        float finalTargetX = targetAngleX + 720f;
        float stopTimer = 0f;
        float stopDuration = 1.0f;

        while (stopTimer < stopDuration)
        {
            stopTimer += Time.deltaTime;
            float t = stopTimer / stopDuration;
            t = 1f - (1f - t) * (1f - t);

            float currentX = Mathf.Lerp(startAngleX, finalTargetX, t);
            reel.localEulerAngles = new Vector3(currentX, initialRotations[index].y, initialRotations[index].z);

            yield return null;
        }

        reel.localEulerAngles = new Vector3(targetAngleX % 360f, initialRotations[index].y, initialRotations[index].z);
    }

    private void UpdateUI()
    {
        if (balanceText != null) balanceText.text = "CREDIT: " + balance.ToString("D4");
        if (betText != null) betText.text = "BET: " + bet.ToString();
    }
}
