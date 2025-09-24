using UnityEngine;
using TMPro;

public class PlayerWallet : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI currentBalance;     // Riferimento alla label che mostra il saldo
    [SerializeField] private int balance = 0;            // Valore del saldo attuale
    public int Balance => balance;                       // Proprietà di sola lettura per altri sistemi

    private void Start()
    {
        UpdateBalance();
    }

    // Prova a spendere una certa somma: ritorna false se non basta il denaro
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;         // importi non positivi non richiedono spesa
        if (balance < amount) return false;   // saldo insufficiente
        balance -= amount;                    // detrae
        UpdateBalance();                      // aggiorna UI
        return true;
    }

    // Aggiunge denaro al portafoglio (ignora importi negativi)
    public void Add(int amount)
    {
        balance += Mathf.Max(0, amount);
        UpdateBalance();
    }

    // Aggiorna il testo della UI con il saldo corrente
    private void UpdateBalance()
    {
        currentBalance.text = "Money: " + balance.ToString() + "$";
    }
}
