
public static class InkEvents
{
    public static event System.Action<float> OnInkConsumed;
    public static event System.Action<float> OnInkRestored;
    public static event System.Action OnBoostInkRecovery;
    public static event System.Action OnResetInkRecovery;
    public static event System.Action OnFullInk;
    public static event System.Action<float> OnItemInkBoostRecovery;


    public static void ConsumeInk(float amount) => OnInkConsumed?.Invoke(amount);
    public static void RestoreInk(float amount) => OnInkRestored?.Invoke(amount);
    public static void BoostInkRecovery() => OnBoostInkRecovery?.Invoke();
    public static void ResetInkRecovery() => OnResetInkRecovery?.Invoke();
    public static void FullInk() => OnFullInk?.Invoke();
    public static void ItemInkBoostRecovery(float amount) => OnItemInkBoostRecovery?.Invoke(amount);


}