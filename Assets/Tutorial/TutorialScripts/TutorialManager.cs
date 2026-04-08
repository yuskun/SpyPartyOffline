using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("依照順序放入 UI")]
    public GameObject[] steps;
    public GameObject Peek;
    public GameObject Banana;
    public GameObject Bat;
    public GameObject Steal;

    private int currentStep = 0;

    // ⭐ 對外開放目前步驟
    public int CurrentStep => currentStep;

    private void Awake()
    {
        Instance = this;
        AutoFindObjects();
        HideObjects();
    }

    void Start()
    {
        ShowStep(0);
    }

    void Update()
    {
        CheckAutoStep(); // 只有部分步驟用自動判斷
        if (Input.GetKeyDown(KeyCode.F2))
        {
            NextStep();
        }
    }

    // =========================
    // UI 控制
    // =========================
    void ShowStep(int step)
    {
        foreach (var s in steps)
        {
            s.SetActive(false);
        }

        if (step < steps.Length)
        {
            steps[step].SetActive(true);
        }

        Debug.Log($"Tutorial Step: {step}");
    }

    public void NextStep()
    {
        currentStep++;

        if (currentStep >= steps.Length)
        {
            Debug.Log("Tutorial Finished");
            return;
        }

        ShowStep(currentStep);
    }

    // =========================
    // 自動判斷（只有移動用）
    // =========================
    void CheckAutoStep()
    {
        switch (currentStep)
        {
            case 0: // Walking
                if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
                {
                    Peek.SetActive(true);
                    NextStep();
                }
                break;
        }
    }

    // =========================
    // ⭐ 操作限制
    // =========================

    public bool CanMove(){ return currentStep >= 0;} // 一開始就能動 
    public bool CanPickPeek(){ return currentStep >= 1;}
    public bool CanUsePeek(){ return currentStep >= 2;}
    public bool CanPickBanana(){ return currentStep >= 3;}
    public bool CanUseBanana(){ return currentStep >= 4;}
    public bool CanPickBat(){ return currentStep >= 5;}
    public bool CanUseBat(){ return currentStep >= 6;}
    public bool CanPickSteal(){ return currentStep >= 7;}
    public bool CanUseSteal(){ return currentStep >= 8;}
    // =========================
    // ⭐ 由外部呼叫
    // =========================

    public void OnPickPeek(){ if (currentStep == 1) NextStep();}
    public void OnUsePeek(){ if (currentStep == 2) Banana.SetActive(true);NextStep();}
    public void OnPickBanana(){ if (currentStep == 3) NextStep();}
    public void OnUseBanana(){ if (currentStep == 4) Bat.SetActive(true);NextStep();}
    public void OnPickBat(){ if (currentStep == 5) NextStep();}
    public void OnUseBat(){ if (currentStep == 6) Steal.SetActive(true);NextStep();}
    public void OnPickSteal(){ if (currentStep == 7) NextStep();}
    public void OnUseSteal(){ if (currentStep == 8) NextStep();}

    void AutoFindObjects()
    {
        if (Peek == null)
            Peek = GameObject.Find("Peek");

        if (Banana == null)
            Banana = GameObject.Find("Banana");

        if (Bat == null)
            Bat = GameObject.Find("Bat");

        if (Steal == null)
            Steal = GameObject.Find("Steal");

        Debug.Log("Auto find tutorial objects done");
    }

    void HideObjects()
    {
        if (Peek == null)
            Peek.SetActive(false);

        if (Banana == null)
            Banana.SetActive(false);

        if (Bat == null)
            Bat.SetActive(false);

        if (Steal == null)
            Steal.SetActive(false);

        Debug.Log("Hide tutorial objects done");
    }

}
