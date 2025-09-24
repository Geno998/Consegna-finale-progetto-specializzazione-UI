using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class BuyQuantityButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Comportamento")]
    [Tooltip("Tempo da tenere premuto prima che inizi la ripetizione (secondi).")]
    public float holdDelay = 0.35f;

    [Tooltip("Frequenza della ripetizione mentre si tiene premuto (secondi per tick).")]
    public float repeatInterval = 0.10f;

    [Header("Eventi")]
    public UnityEvent onClickTap;   // Eseguito una volta se si rilascia prima di holdDelay (tap)
    public UnityEvent onRepeat;     // Eseguito ripetutamente dopo holdDelay finché il pulsante è premuto

    // Stato runtime per gestire interazione e tempistiche
    private bool _isDown;
    private bool _isHolding;
    private float _downTime;
    private float _nextRepeatAt;

    // Quando si preme: memorizza l'istante e resetta lo stato di holding
    public void OnPointerDown(PointerEventData eventData)
    {
        _isDown = true;
        _isHolding = false;
        _downTime = Time.unscaledTime;
        _nextRepeatAt = float.PositiveInfinity;
    }

    // Quando si rilascia: se non si è entrati nella fase di hold, invoca il tap singolo
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDown) return;

        // Se non abbiamo superato holdDelay -> è un tap
        if (!_isHolding)
            onClickTap?.Invoke();

        _isDown = false;
        _isHolding = false;
    }

    // Se il puntatore esce dal bottone: annulla l'interazione corrente
    public void OnPointerExit(PointerEventData eventData)
    {
        // Uscire dall'area del bottone cancella l'input in corso
        _isDown = false;
        _isHolding = false;
    }

    // Gestione del passaggio da tap a hold e dei tick di ripetizione
    private void Update()
    {
        if (!_isDown) return;

        float t = Time.unscaledTime;

        if (!_isHolding)
        {
            // Passa in stato holding dopo holdDelay
            if (t - _downTime >= holdDelay)
            {
                _isHolding = true;
                _nextRepeatAt = t;
            }
        }
        else
        {
            // Durante holding: invoca onRepeat a ogni intervallo
            if (t >= _nextRepeatAt)
            {
                onRepeat?.Invoke();
                _nextRepeatAt = t + repeatInterval;
            }
        }
    }
}
