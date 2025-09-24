using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class TestAddItems : MonoBehaviour
{
    [SerializeField] private List<sSurv1ItemData> possibleItemsData = new();
    [SerializeField] private int itemToSpawn;
    [SerializeField] private int numberToSpawn;

    [SerializeField] private TMP_InputField numberField;

    [SerializeField] private TMP_InputField IDField;


    [SerializeField] sSurv1MenuManager itemManager;
    [SerializeField] sSurv1TaskbarManager TaskbarManager;

    int number;
    int ID;

    public void SpawnItemButton()
    {


        string numberString = numberField.text;

        if (int.TryParse(numberField.text, out int result))
        {
            number = result;
        }

        string IDString = IDField.text;

        if (int.TryParse(IDField.text, out int result2))
        {
            ID = result2;
        }

        int notPlaced = itemManager.AddItemToInventory(possibleItemsData[ID].ItemID, number);
        if (notPlaced > 0)
        {
            notPlaced = TaskbarManager.AddItemToTaskbar(possibleItemsData[ID].ItemID, notPlaced);
        }
    }



}
